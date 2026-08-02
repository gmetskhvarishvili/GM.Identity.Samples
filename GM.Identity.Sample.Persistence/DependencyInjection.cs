using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Identity.Sample.Persistence.Context;
using GM.Identity.Sample.Persistence.Repositories;
using GM.Messaging.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GM.Identity.Sample.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEntityFrameworkNpgsql();
        
        services.AddDbContextPool<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ApplicationDatabase"),
                o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    o.CommandTimeout(60);
                });
            options.UseInternalServiceProvider(serviceProvider);
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
        services.AddTransient<ITwoFactorAuthTypeRepository, TwoFactorAuthTypeRepository>();
        services.AddTransient<IUserSessionRepository, UserSessionRepository>();
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IUserRoleRepository, UserRoleRepository>();
        services.AddTransient<IUserSessionRepository, UserSessionRepository>();
        services.AddTransient<IUserTwoFactorAuthTypeRepository, UserTwoFactorAuthTypeRepository>();
        services.AddTransient<IUnitOfWork, UnitOfWork.UnitOfWork>();
        
        services.AddTransient<OutboxMessageRepository>();
        services.AddTransient<IOutboxMessageRepository>(sp => sp.GetRequiredService<OutboxMessageRepository>());
        services.AddTransient<IOutboxDbContext<OutboxMessage>>(sp => sp.GetRequiredService<OutboxMessageRepository>());

        return services;
    }
}