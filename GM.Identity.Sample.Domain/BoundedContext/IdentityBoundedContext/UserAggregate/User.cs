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
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.Events.Users;
using GM.Identity.Domain.Identity.UserAggregate.Entities;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;

public class User : GMUser
<Client, ClientSession, ClientScope, Scope, ScopeOperation, Operation,
    User, UserSession, TwoFactorAuthType, UserTwoFactorAuthType,
    UserRole, Role, RolePermission, Permission>, IAggregateRoot
{
    private User()
    {

    }

    private User(
        string username,
        string? email,
        string? phoneNumber) : base(
        username,
        email,
        phoneNumber)
    {
    }

    public static User Create(
        string username,
        string? email,
        string? phoneNumber)
    {
        return new User(username, email, phoneNumber);
    }

    /// <summary>
    /// Sets a new password hash/salt and raises <see cref="UserPasswordChangedDomainEvent"/>. The event (in the
    /// domain-event store) is the record of the change and the basis for password-expiry — use this rather than
    /// the base <c>UpdatePassword</c> for any user-initiated or admin password change.
    /// </summary>
    public void ChangePassword(string passwordHash, string passwordSalt)
    {
        UpdatePassword(passwordHash, passwordSalt);
        RaiseDomainEvent(new UserPasswordChangedDomainEvent(Id));
    }
}
