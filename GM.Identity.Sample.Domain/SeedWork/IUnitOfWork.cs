using GM.EntityFramework.Domain.Repositories;
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
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;

namespace GM.Identity.Sample.Domain.SeedWork;

public interface IUnitOfWork : IGenericUnitOfWork
{
    public IClientRepository ClientRepository { get; }
    
    public IClientSessionRepository ClientSessionRepository { get; }
    
    public IClientScopeRepository ClientScopeRepository { get; }
    public IScopeRepository ScopeRepository { get; }
    public IScopeOperationRepository ScopeOperationRepository { get; }
    public IOperationRepository OperationRepository { get; }
    
    
    public IUserRepository UserRepository { get; }
   
    public IUserSessionRepository UserSessionRepository { get; }
   
    public ITwoFactorAuthTypeRepository TwoFactorAuthTypeRepository { get; }
    public IUserTwoFactorAuthTypeRepository UserTwoFactorAuthTypeRepository { get; }
  
    public IUserRoleRepository UserRoleRepository { get; }
    public IRoleRepository RoleRepository { get; }
    public IPermissionRepository PermissionRepository { get; }
    public IRolePermissionRepository RolePermissionRepository { get; }
    
    public IOutboxMessageRepository OutboxMessageRepository { get; }
}