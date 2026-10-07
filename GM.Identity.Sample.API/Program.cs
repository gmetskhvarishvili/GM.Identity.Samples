using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using GM.API.Startup;
using GM.Identity;
using GM.API.Authorization;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using GM.Identity.Authorization;
using GM.Identity.Sample.Infrastructure;
using GM.Identity.Sample.Persistence;
using GM.Identity.Sample.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = ProgramExtension.CreateGMBuilder(args);

builder.Services.ConfigureGMServices(
    builder.Configuration,
    "policyName",
    "SwaggerDocOptions");

builder.Services.AddGMIdentity();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// OpenTelemetry: request/HTTP/DB tracing + ASP.NET/HTTP/runtime metrics, exported over OTLP. The trace
// context flows from the gateway (W3C traceparent), so a request is one connected distributed trace.
// Point it at a collector via OTEL_EXPORTER_OTLP_ENDPOINT (default http://localhost:4317).
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("GM.Identity.Sample.API", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Npgsql")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();

app.UseGMServices();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (context != null)
        {
            if ((await context.Database.GetPendingMigrationsAsync()).Any())
            {
                await context.Database.MigrateAsync();
            }

            var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContextSeed>>();
            if (logger != null)
            {
                // Permission names come from the [HasPermission] attributes on controller actions (one per
                // gated action, name = action name). Reflection lives here in the API (the seed, in the
                // Persistence layer, can't see the controllers). The Redis cache is optional (null = no Redis).
                var permissionNames = typeof(Program).Assembly.GetTypes()
                    .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    .SelectMany(m => m.GetCustomAttributes<HasPermissionAttribute>())
                    .Select(a => a.PermissionName)
                    .Distinct()
                    .ToList();

                var permissionCache = scope.ServiceProvider.GetService<IPermissionCache>();
                var scopeCache = scope.ServiceProvider.GetService<IScopeCache>();
                var adminPassword = builder.Configuration["Seed:AdminPassword"];
                var clientSecret = builder.Configuration["Seed:ClientSecret"];
                Guid? clientId = Guid.TryParse(builder.Configuration["Seed:ClientId"], out var parsedClientId)
                    ? parsedClientId
                    : null;
                var seedConsentDocuments = builder.Configuration.GetValue("Seed:ConsentDocuments", true);

                await new ApplicationDbContextSeed().SeedAsync(
                    context, logger, permissionNames, permissionCache, adminPassword, clientId, clientSecret, scopeCache,
                    seedConsentDocuments);
            }
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
    }
}

await app.RunAsync();

// Exposes the implicit top-level Program class to the test project (GmWebApplicationFactory<Program>).
public partial class Program;
