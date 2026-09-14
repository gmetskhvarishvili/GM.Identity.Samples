using GM.HealthChecks.Caching;
using GM.HealthChecks.DistributedLock;
using GM.HealthChecks.EntityFramework;
using GM.HttpClient;
using GM.Identity;
using GM.Identity.Sample.Application.Common;
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

namespace GM.Identity.Sample.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OAuthOptions>(configuration.GetSection("OAuth"));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<PasswordPolicyOptions>(configuration.GetSection(PasswordPolicyOptions.SectionName));

        services.AddScoped<IOAuthService, OAuthService>();
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
