using GM.EntityFramework.Domain.Events;
using GM.Identity.Domain.AccessControl.ClientScopeAggregate.Events;
using GM.Identity.Domain.AccessControl.OperationAggregate.Events;
using GM.Identity.Domain.AccessControl.RolePermissionAggregate.Events;
using GM.Identity.Domain.AccessControl.ScopeOperationAggregate.Events;
using GM.Identity.Domain.Authorization.ClientSessionAggregate.Events;
using GM.Identity.Domain.Authorization.UserSessionAggregate.Events;
using GM.HealthChecks.Caching;
using GM.HealthChecks.DistributedLock;
using GM.HealthChecks.EntityFramework;
using GM.HttpClient;
using GM.Identity;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Oidc;
using GM.Identity.Sample.Application.Infrastructure.Services.OAuth;
using GM.Identity.Sample.Application.Infrastructure.Services.OTP;
using GM.Identity.Domain.AccessControl.UserRoleAggregate.Events;
using GM.Identity.Sample.Infrastructure.Authorization;
using GM.Identity.Sample.Infrastructure.Options;
using GM.Identity.Sample.Infrastructure.Services.OAuth;
using GM.Identity.Sample.Infrastructure.Services.OTP;
using GM.Identity.Sample.Persistence.Context;
using GM.Messaging;
using GM.OTP;
using GM.Scheduling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GM.Identity.Sample.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OAuthOptions>(configuration.GetSection("OAuth"));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<PasswordPolicyOptions>(configuration.GetSection(PasswordPolicyOptions.SectionName));

        // Expose the bound options to the Application layer through its interfaces (Application can't reference the
        // concrete option classes, which live here in Infrastructure).
        services.AddSingleton<IAuthOptions>(sp => sp.GetRequiredService<IOptions<AuthOptions>>().Value);
        services.AddSingleton<IPasswordPolicyOptions>(sp => sp.GetRequiredService<IOptions<PasswordPolicyOptions>>().Value);

        // Key for PII-at-rest encryption (configure DataProtection:Key in production; dev key otherwise).
        services.AddSingleton(new EncryptionKeyProvider(configuration["DataProtection:Key"]));
        services.Configure<RetentionOptions>(configuration.GetSection(RetentionOptions.SectionName));

        // Breached-password checking: HIBP when enabled, otherwise a no-op checker.
        if (configuration.GetValue<bool>($"{PasswordPolicyOptions.SectionName}:{nameof(PasswordPolicyOptions.CheckForBreaches)}"))
        {
            services.AddHttpClient<IBreachedPasswordChecker, HibpBreachedPasswordChecker>(client =>
                client.BaseAddress = new System.Uri("https://api.pwnedpasswords.com/"));
        }
        else
        {
            services.AddSingleton<IBreachedPasswordChecker, NullBreachedPasswordChecker>();
        }

        services.AddScoped<IOAuthService, OAuthService>();

        // OIDC back-channel logout: a stable OP signing key (published via JWKS so RPs can verify), the logout
        // token generator, and the notifier that POSTs tokens to relying parties on Single Logout.
        services.Configure<OidcSigningOptions>(configuration.GetSection(OidcSigningOptions.SectionName));
        services.AddSingleton(sp => new OidcSigningKey(
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OidcSigningOptions>>().Value));
        services.AddSingleton<IJwksProvider>(sp => sp.GetRequiredService<OidcSigningKey>());
        services.AddSingleton<LogoutTokenGenerator>();
        services.AddSingleton<IIdTokenGenerator, IdTokenGenerator>();
        services.AddSingleton<IIdTokenReader, IdTokenReader>();
        services.AddScoped<IBackchannelLogoutNotifier, BackchannelLogoutNotifier>();
        services.AddScoped<IOTPService, OTPService>();
        services.AddGMTotp();
        services.AddGMHttpClient<IOTPAPIService, GMAPIClientOptions>(
            configuration.GetSection("ApiServices:OTPAPIService"),
            "OTPAPIService");

        services.AddHealthChecks()
            .AddGMDatabaseCheck<ApplicationDbContext>()
            .AddGMCacheCheck()
            .AddGMDistributedLockCheck();

        // RBAC/scope/session authorization caches (GM.Identity, over the IConnectionMultiplexer registered
        // by AddGMRedisCaching) plus the reconciliation jobs that rebuild them from the DB. Both require
        // Redis, so they are only wired when a Redis connection string is configured. The caches are
        // library types; the reconcile jobs stay in the sample (they bridge the caches to the app's repos).
        if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString("Redis")))
        {
            services.AddGMIdentityRedisAuthorization();
            services.AddGMScheduling(builder =>
                builder.ScanAssemblies(new[] { typeof(PermissionCacheReconciliationJob).Assembly }));

            // Project role-grant changes into the Redis RBAC cache off domain events. Registered alongside the
            // caches/scheduler they depend on, so they're only wired when Redis is configured. A grant is
            // written through synchronously; a revocation is evicted asynchronously via UserRoleCacheEvictionJob
            // (auto-discovered by the scan above).
            services.AddScoped<IDomainEventHandler<GMUserRoleCreatedDomainEvent>, UserRoleCreatedPermissionCacheHandler>();
            services.AddScoped<IDomainEventHandler<GMUserRoleDeletedDomainEvent>, UserRoleDeletedCacheEvictionHandler>();

            // Role-permission grant changes are applied to the RBAC cache asynchronously via the
            // RolePermissionCache*Job jobs (auto-discovered by the scan above), off the library-raised
            // GMRolePermissionCreated/DeletedDomainEvents.
            services.AddScoped<IDomainEventHandler<GMRolePermissionCreatedDomainEvent>, RolePermissionCreatedCacheProjectionHandler>();
            services.AddScoped<IDomainEventHandler<GMRolePermissionDeletedDomainEvent>, RolePermissionDeletedCacheEvictionHandler>();

            // A revoked user session is evicted from the Redis session cache asynchronously via
            // UserSessionCacheEvictionJob (auto-discovered by the scan above), off the library-raised
            // GMUserSessionRevokedDomainEvent.
            services.AddScoped<IDomainEventHandler<GMUserSessionRevokedDomainEvent>, UserSessionRevokedCacheEvictionHandler>();

            // Client-session eviction, and scope-cache projection/eviction (client-scope, scope-operation, and the
            // operation name→id map), all deferred to jobs (auto-discovered by the scan above) off library events.
            services.AddScoped<IDomainEventHandler<GMClientSessionRevokedDomainEvent>, ClientSessionRevokedCacheEvictionHandler>();
            services.AddScoped<IDomainEventHandler<GMClientScopeCreatedDomainEvent>, ClientScopeCreatedCacheProjectionHandler>();
            services.AddScoped<IDomainEventHandler<GMClientScopeDeletedDomainEvent>, ClientScopeDeletedCacheEvictionHandler>();
            services.AddScoped<IDomainEventHandler<GMScopeOperationCreatedDomainEvent>, ScopeOperationCreatedCacheProjectionHandler>();
            services.AddScoped<IDomainEventHandler<GMScopeOperationDeletedDomainEvent>, ScopeOperationDeletedCacheEvictionHandler>();
            services.AddScoped<IDomainEventHandler<GMOperationCreatedDomainEvent>, OperationCreatedScopeCacheProjectionHandler>();
        }

        return services;
    }

    public static IServiceCollection AddProducerWorkerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGMMessaging(configuration);
        return services;
    }
}
