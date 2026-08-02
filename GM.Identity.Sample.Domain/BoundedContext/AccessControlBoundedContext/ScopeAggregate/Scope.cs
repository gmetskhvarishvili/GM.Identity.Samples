using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Domain.Entities;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate;

public class Scope
    : GMScope<Client, ClientSession, ClientScope, Scope, ScopeOperation, Operation,
            User, UserSession, TwoFactorAuthType, UserTwoFactorAuthType,
            UserRole, Role, RolePermission, Permission>
        ,  IAggregateRoot
{
    private Scope(
        string name) : base(name)
    {
    }
    
    public static Scope Create(
        string name)
    {
        return new Scope
            (name);
    }
}