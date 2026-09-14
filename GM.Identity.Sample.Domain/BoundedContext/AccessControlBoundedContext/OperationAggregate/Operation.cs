using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Identity.Domain.AccessControl.OperationAggregate.Entities;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate;

public class Operation
    : GMOperation<Client, ClientSession, ClientScope, Scope, ScopeOperation, Operation,
            User, UserSession, TwoFactorAuthType, UserTwoFactorAuthType,
            UserRole, Role, RolePermission, Permission>,
        IAggregateRoot, IHasTenant
{
    private Operation() // EF Core materialization
    {
    }

    private Operation(
        string name,
        string description) : base(name, description)
    {
    }

    public static Operation Create(
        string name,
        string description)
    {
        return new Operation(name, description);
    }

    /// <summary>The tenant this operation belongs to, or <c>null</c> for a global (cross-tenant) row.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Assigns the owning tenant (stamped by the persistence layer on insert when unset).</summary>
    public void AssignTenant(Guid? tenantId) => TenantId = tenantId;
}
