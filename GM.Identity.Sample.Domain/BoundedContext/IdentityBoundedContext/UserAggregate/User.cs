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
        string? phoneNumber) => new(username, email, phoneNumber);

    /// <summary>Optional personal details, held 1:1 and owned by the user aggregate.</summary>
    public UserPersonalInfo? PersonalInfo { get; private set; }

    /// <summary>Sets or updates the user's personal details (creates the 1:1 row on first use).</summary>
    public void SetPersonalInfo(string? firstName, string? lastName, string? personalNumber, DateTime? birthDate)
    {
        if (PersonalInfo is null)
            PersonalInfo = UserPersonalInfo.Create(Id, firstName, lastName, personalNumber, birthDate);
        else
            PersonalInfo.Update(firstName, lastName, personalNumber, birthDate);
    }
}
