using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using System;
using GM.Caching.Redis;
using GM.DistributedLock.Redis;
using GM.Gateway;
using GM.HealthChecks;
using GM.HealthChecks.Caching;
using GM.HealthChecks.DistributedLock;
using GM.Identity.Sample.Gateway.API;
using GM.Identity.Authorization;
using GM.RateLimiting.Http;
using GM.RateLimiting.Redis;
using Microsoft.AspNetCore.Authentication;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------------------
// Backing store: Redis-backed cache + distributed lock, plus the lock-free atomic Redis rate-limit
// store so edge limits are enforced consistently across every gateway instance.
// ---------------------------------------------------------------------------------------------
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis is required (set ConnectionStrings:Redis or env ConnectionStrings__Redis).");

builder.Services.AddGMRedisCaching(options =>
{
    options.ConnectionString = redisConnection;
    options.KeyPrefix = "identity-gw:";
});
builder.Services.AddGMRedisDistributedLock(options => options.ConnectionString = redisConnection);
builder.Services.AddGMRedisRateLimitStore(redisConnection);

// ---------------------------------------------------------------------------------------------
// YARP reverse proxy + GM edge rate limiting, both loaded from configuration (see appsettings.json).
// Routes opt into a rate-limit policy and an authorization policy through their Metadata â€” no code
// per route.
// ---------------------------------------------------------------------------------------------
builder.Services.AddGMGateway(
        builder.Configuration,
        configureHttp: options => options.DefaultPartition = RateLimitPartition.Ip)
    // Inject the caller's identity from the validated token into trusted X-* headers the backend reads
    // (client-supplied copies are stripped first so they cannot be spoofed).
    .AddGMActorForwarding()
    // "Current user" routes: forward /me and /me/sessions to the backend's owner-scoped self endpoints.
    // The backend resolves the caller from the gateway-injected X-User-Id header (not the path), so a
    // caller can only ever read their own info/sessions — and these endpoints need no admin permission.
    .AddTransforms(context =>
    {
        if (context.Route.RouteId is not ("me" or "me-sessions")) return;

        var toSessions = context.Route.RouteId == "me-sessions";
        context.AddRequestTransform(transformContext =>
        {
            transformContext.Path = toSessions ? "/api/v1.0/users/me/Sessions" : "/api/v1.0/users/me";
            return ValueTask.CompletedTask;
        });
    });

// ---------------------------------------------------------------------------------------------
// Edge authentication: validate the opaque bearer token against the Redis session store and populate
// the principal (NameIdentifier=userId, session_id). AddGMActorForwarding above then turns those claims
// into the trusted X-User-Id / X-Session-Id headers for the backend, and AuthorizationPolicy="default"
// routes (/me, /me/sessions) get an authenticated user. Anonymous routes (/connect, create-user) carry
// no token and pass through untouched.
// ---------------------------------------------------------------------------------------------
builder.Services.AddAuthentication(OpaqueTokenAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, OpaqueTokenAuthenticationHandler>(
        OpaqueTokenAuthenticationHandler.SchemeName, configureOptions: null);

// ---------------------------------------------------------------------------------------------
// Authorization: routes opt into the built-in "default" policy (an authenticated user) via their
// Metadata (AuthorizationPolicy). Authentication is the opaque-token handler above; there are no
// custom edge policies — permission/scope enforcement lives on the backend API.
// ---------------------------------------------------------------------------------------------
builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------------------------
// Health: liveness self-check + Redis cache and distributed-lock readiness.
// ---------------------------------------------------------------------------------------------
builder.Services.AddGMHealthChecks()
    .AddGMCacheCheck()
    .AddGMDistributedLockCheck();

// Distributed tracing/metrics over OTLP; the W3C trace context is propagated to the backend so the
// gateway and API spans join into one trace. Collector endpoint via OTEL_EXPORTER_OTLP_ENDPOINT.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("GM.Identity.Sample.Gateway.API", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// HTTP client used to fetch the backend's OpenAPI document for the gateway's Swagger. Dev-only cert
// bypass, matching the reverse-proxy cluster.
builder.Services.AddHttpClient("identity-docs")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    });

var app = builder.Build();

// An auth handler that reads a form POST body (e.g. to extract access_token) would consume the request
// body before YARP can forward it (leaving the proxy with 0 bytes vs the promised Content-Length).
// Buffer form POSTs so the body can be re-read, and rewind it before the proxy runs.
app.Use(async (context, next) =>
{
    if (HttpMethods.IsPost(context.Request.Method) && context.Request.HasFormContentType)
        context.Request.EnableBuffering();

    await next();
});

app.UseRouting();

// Swagger UI at the root URL (no /swagger prefix), rendering the Identity API's OpenAPI document
// proxied through the gateway (see the "identity-docs" route). Served anonymously, before auth;
// "Try it out" calls go back through the gateway origin, so edge auth + rate limiting apply.
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/api-docs/identity/v1.json", "Identity API v1");
    options.RoutePrefix = string.Empty;
    options.DocumentTitle = "GM Identity Gateway";
});

app.UseAuthentication();
app.UseAuthorization();

// Rewind the (buffered) body the auth pipeline may have consumed, so the proxy forwards it intact.
app.Use(async (context, next) =>
{
    if (context.Request.Body.CanSeek)
        context.Request.Body.Position = 0;

    await next();
});

// The gateway's filtered OpenAPI document (served to the root Swagger UI).
app.MapGatewayOpenApiDoc();

// /health/live, /health/ready, /health/startup.
app.MapGMHealthChecks();

// Reverse proxy pipeline: per-route rate limiting enforced at the edge before forwarding.
app.MapGMGateway();

await app.RunAsync();
