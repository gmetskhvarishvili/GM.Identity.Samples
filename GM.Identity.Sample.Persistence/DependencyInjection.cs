using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.GroupAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleHierarchyAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.TimeBoundRoleGrantAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserPermissionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ApiKeyAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserConsentAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPasswordHistoryAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPendingContactChangeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTotpDeviceAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserRecoveryCodeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.EmailTemplateAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;
using GM.Identity.Sample.Application.Infrastructure.Services.Audit;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Identity.Sample.Persistence.Context;
using GM.Identity.Sample.Persistence.Repositories;
using GM.Identity.Sample.Persistence.Services.Audit;
using GM.Caching.Redis;
using GM.DistributedLock.Redis;
using GM.EntityFramework.Persistence;
using GM.Messaging.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GM.Identity.Sample.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddGMRedisCaching(options =>
            {
                options.ConnectionString = redis;
                options.KeyPrefix = "gm-identity:";
            });
            services.AddGMRedisDistributedLock(options => options.ConnectionString = redis);
        }

        // Not pooled: the tenant global query filter (see ApplicationDbContext) references the ambient
        // ICurrentActor, so each context must resolve it per scope. Pooling reuses instances and only
        // supports a DbContextOptions-only constructor, which would prevent injecting the actor.
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ApplicationDatabase"),
                o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    o.CommandTimeout(60);
                });

            // Tenant-owned aggregates (IHasTenant) carry a global query filter but their required
            // dependents don't; that pairing is intentional here, so silence EF's advisory warning.
            options.ConfigureWarnings(w =>
                w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));

            options.AddGMActorAuditing(serviceProvider);
        });

        services.AddTransient<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddTransient<IClientRepository, ClientRepository>();
        services.AddTransient<IClientSessionRepository, ClientSessionRepository>();
        services.AddTransient<IClientScopeRepository, ClientScopeRepository>();
        services.AddTransient<IScopeRepository, ScopeRepository>();
        services.AddTransient<IScopeOperationRepository, ScopeOperationRepository>();
        services.AddTransient<IOperationRepository, OperationRepository>();
        services.AddTransient<IPermissionRepository, PermissionRepository>();
        services.AddTransient<IRolePermissionRepository, RolePermissionRepository>();
        services.AddTransient<IRoleRepository, RoleRepository>();
        services.AddTransient<IRoleHierarchyRepository, RoleHierarchyRepository>();
        services.AddTransient<IGroupRepository, GroupRepository>();
        services.AddTransient<IGroupRoleRepository, GroupRoleRepository>();
        services.AddTransient<IUserGroupRepository, UserGroupRepository>();
        services.AddTransient<ITwoFactorAuthTypeRepository, TwoFactorAuthTypeRepository>();
        services.AddTransient<IUserSessionRepository, UserSessionRepository>();
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IUserRoleRepository, UserRoleRepository>();
        services.AddTransient<IUserSessionRepository, UserSessionRepository>();
        services.AddTransient<IUserTwoFactorAuthTypeRepository, UserTwoFactorAuthTypeRepository>();
        services.AddTransient<IUserRecoveryCodeRepository, UserRecoveryCodeRepository>();
        services.AddTransient<IUserTotpDeviceRepository, UserTotpDeviceRepository>();
        services.AddTransient<IUserPasswordHistoryRepository, UserPasswordHistoryRepository>();
        services.AddTransient<IUserPendingContactChangeRepository, UserPendingContactChangeRepository>();
        services.AddTransient<IUserPermissionRepository, UserPermissionRepository>();
        services.AddTransient<ITimeBoundRoleGrantRepository, TimeBoundRoleGrantRepository>();
        services.AddTransient<IClientRedirectUriRepository, ClientRedirectUriRepository>();
        services.AddTransient<IAuthorizationCodeRepository, AuthorizationCodeRepository>();
        services.AddTransient<ISsoSessionRepository, SsoSessionRepository>();
        services.AddTransient<IApiKeyRepository, ApiKeyRepository>();
        services.AddTransient<IUserConsentRepository, UserConsentRepository>();
        services.AddTransient<IUserPasskeyRepository, UserPasskeyRepository>();
        services.AddTransient<IPasskeyChallengeRepository, PasskeyChallengeRepository>();
        services.AddTransient<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddTransient<IUnitOfWork, UnitOfWork.UnitOfWork>();

        // Read-side over the durable domain-event log (the audit trail).
        services.AddTransient<IAuditTrailReader, AuditTrailReader>();

        services.AddTransient<OutboxMessageRepository>();
        services.AddTransient<IOutboxMessageRepository>(sp => sp.GetRequiredService<OutboxMessageRepository>());
        services.AddTransient<IOutboxDbContext<OutboxMessage>>(sp => sp.GetRequiredService<OutboxMessageRepository>());

        return services;
    }
}
