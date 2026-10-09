using System;
using System.Net.Http;
using GM.API.Startup;
using GM.Identity.Sample.Service.API.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// A *resource server* sample: it does not issue tokens. Same GM bootstrap as the provider API, but with no
// database — it validates the provider's opaque access tokens and enforces the user's permissions and the
// client's scopes entirely out of the shared Redis projections.
var builder = ProgramExtension.CreateGMBuilder(args);

// Framework services: controllers, Swagger (with the OAuth2 flows from SwaggerDocOptions), ICurrentActor,
// the event source, CORS, and a scheme-less authentication/authorization pipeline — same as the provider.
builder.Services.ConfigureGMServices(
    builder.Configuration,
    "policyName",
    "SwaggerDocOptions");

// Everything this service needs from the provider goes over HTTP — it holds no session / RBAC / scope state
// and needs no Redis. Authentication validates the Bearer token via RFC 7662 introspection; authorization
// ([RequirePermission] / [RequireScope]) asks the provider's authz endpoints. The provider is the source of
// truth for tokens, permissions and scopes.
var identityApiBaseUrl = builder.Configuration["IdentityApi:BaseUrl"]
    ?? throw new InvalidOperationException("IdentityApi:BaseUrl is required (the Identity provider's base URL).");

// In-memory cache for introspection results (short TTL, capped by the token's expiry) so a hot token isn't
// introspected on every request. Per-instance and dependency-free — no Redis.
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<IIdentityProviderClient, IdentityProviderClient>(client =>
        client.BaseAddress = new Uri(identityApiBaseUrl))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Dev-only: trust the provider's self-signed certificate (matches the gateway clusters).
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    });

builder.Services.AddAuthentication(IntrospectionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, IntrospectionAuthenticationHandler>(
        IntrospectionAuthenticationHandler.SchemeName, configureOptions: null);

// Distributed tracing/metrics over OTLP, same shape as the provider; collector via OTEL_EXPORTER_OTLP_ENDPOINT.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("GM.Identity.Sample.Service.API", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();

app.UseGMServices();

await app.RunAsync();

// Exposes the implicit top-level Program class to a test project, mirroring the provider API.
public partial class Program;
