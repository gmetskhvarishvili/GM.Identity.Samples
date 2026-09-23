using GM.HealthChecks.Caching;
using GM.HealthChecks.DistributedLock;
using GM.HealthChecks.EntityFramework;
using GM.HttpClient;
using GM.Identity;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Oidc;
using GM.Identity.Sample.Application.Infrastructure.Services.OAuth;
using GM.Identity.Sample.Application.Infrastructure.Services.OTP;
using GM.Identity.Sample.Infrastructure.Authorization;
using GM.Identity.Sample.Infrastructure.Options;
using GM.Identity.Sample.Infrastructure.Services.OAuth;
using GM.Identity.Sample.Infrastructure.Services.OTP;
using GM.Identity.Sample.Persistence.Context;
using GM.Messaging;
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
        }

        return services;
    }

    public static IServiceCollection AddProducerWorkerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGMMessaging(configuration);
        return services;
    }
}
