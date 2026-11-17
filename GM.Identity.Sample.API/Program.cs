using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using GM.API.Startup;
using GM.Identity;
using GM.API.Authorization;
using GM.Identity.Authorization;
using GM.Messaging.AspNetCore;
using GM.Identity.Sample.Infrastructure;
using GM.Identity.Sample.Persistence;
using GM.Identity.Sample.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Http;
using GM.Idempotency;
using GM.Identity.Sample.API.Idempotency;
using GM.API;
using GM.EntityFramework.Domain.Common;
using GM.Identity.Sample.API.CurrentActor;
using GM.Identity.Sample.Application.Common;

var builder = ProgramExtension.CreateGMBuilder(args);

builder.Services.ConfigureGMServices(
    builder.Configuration,
    "policyName",
    "SwaggerDocOptions");

builder.Services.AddGMIdentity();

// Never store /connect (token) responses for idempotent replay — see TokenSafeIdempotencyKeyProvider.
builder.Services.Replace(
    ServiceDescriptor.Singleton<IIdempotencyKeyProvider<HttpContext>, TokenSafeIdempotencyKeyProvider>());

// Let sign-in handlers name the caller of an anonymous request — see EstablishableCurrentActor.
builder.Services.AddSingleton<HttpContextCurrentActor>();
builder.Services.AddSingleton<EstablishableCurrentActor>();
builder.Services.Replace(
    ServiceDescriptor.Singleton<ICurrentActor>(sp => sp.GetRequiredService<EstablishableCurrentActor>()));
builder.Services.AddSingleton<IActorEstablisher>(sp => sp.GetRequiredService<EstablishableCurrentActor>());

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// GM.Messaging's admin endpoints (/admin/messaging) are for administrators: a bearer token this API issued (validated
// like at the gateways, by GM.Identity's opaque-token scheme) whose user holds the MessagingAdmin permission through one
// of their roles (the RBAC behind [HasPermission]; seeded below and granted to the Administrator role).
const string MessagingAdmin = "MessagingAdmin";
// Only the policy authenticates with the scheme: the rest of the API keeps reading the gateway-forwarded actor headers.
AppContext.SetSwitch("Microsoft.AspNetCore.Authentication.SuppressAutoDefaultScheme", true);
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, OpaqueTokenAuthenticationHandler>(OpaqueTokenAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(MessagingAdmin, policy => policy
        .AddAuthenticationSchemes(OpaqueTokenAuthenticationHandler.SchemeName)
        .RequireAuthenticatedUser()
        .RequireAssertion(async context =>
        {
            if (context.Resource is not HttpContext http
                || !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return false;

            var permissions = http.RequestServices.GetRequiredService<IPermissionCache>();
            return await permissions.GetPermissionIdByNameAsync(MessagingAdmin, http.RequestAborted) is { } permissionId
                && await permissions.HasPermissionAsync(userId, permissionId, http.RequestAborted);
        }));

var app = builder.Build();

app.UseGMServices();
app.MapGMMessagingAdmin(admin => admin.AuthorizationPolicy = MessagingAdmin);

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
                // Persistence layer, can't see the controllers), plus MessagingAdmin for the messaging admin
                // endpoints. The Redis cache is optional (null = no Redis).
                var permissionNames = typeof(Program).Assembly.GetTypes()
                    .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    .SelectMany(m => m.GetCustomAttributes<HasPermissionAttribute>())
                    .Select(a => a.PermissionName)
                    .Append(MessagingAdmin)
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
