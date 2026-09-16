using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.EntityFramework.Domain.Common;
using GM.Identity.Domain.Identity.ClientAggregate.Entities;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;

public class Client : GMClient<Client, ClientSession, ClientScope, Scope, ScopeOperation, Operation,
    User, UserSession, TwoFactorAuthType, UserTwoFactorAuthType,
    UserRole, Role, RolePermission, Permission>, IAggregateRoot, IHasTenant
{
    private Client() // EF Core materialization
    {
    }

    private Client(
        string name)
        : base(
            name)
    {
    }

    public static Client Create(
        string name)
    {
        return new Client(name);
    }

    /// <summary>The tenant this client belongs to, or <c>null</c> for a global (cross-tenant) row.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Assigns the owning tenant (stamped by the persistence layer on insert when unset).</summary>
    public void AssignTenant(Guid? tenantId) => TenantId = tenantId;

    /// <summary>
    /// The client's OpenID Connect back-channel logout endpoint. When set, the OP POSTs a signed logout token
    /// here on Single Logout so this relying party can terminate its own session. Null = the client does not
    /// participate in back-channel logout.
    /// </summary>
    public string? BackchannelLogoutUri { get; private set; }

    public void SetBackchannelLogoutUri(string? uri) => BackchannelLogoutUri = uri;

    /// <summary>
    /// The client's OpenID Connect front-channel logout endpoint. When set, Single Logout returns this URL (with
    /// <c>iss</c> and <c>sid</c>) for the user agent to load in a hidden iframe so the relying party can clear
    /// its own session in the browser. Null = the client does not participate in front-channel logout.
    /// </summary>
    public string? FrontchannelLogoutUri { get; private set; }

    public void SetFrontchannelLogoutUri(string? uri) => FrontchannelLogoutUri = uri;

    /// <summary>
    /// When true, the authorization endpoint requires the user's explicit consent to the requested scopes before
    /// issuing a code (unless a prior consent already covers them). Default false — first-party clients skip it.
    /// </summary>
    public bool RequireConsent { get; private set; }

    public void SetRequireConsent(bool requireConsent) => RequireConsent = requireConsent;
}
