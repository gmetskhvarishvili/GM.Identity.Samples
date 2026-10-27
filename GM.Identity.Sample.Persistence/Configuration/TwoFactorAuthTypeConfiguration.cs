using GM.Identity.Persistence.Configuration;
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
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace GM.Identity.Sample.Persistence.Configuration;

public class TwoFactorAuthTypeConfiguration: GMTwoFactorAuthTypeConfiguration
<Client, ClientSession, ClientScope, Scope, ScopeOperation, Operation,
    User, UserSession, TwoFactorAuthType, UserTwoFactorAuthType,
    UserRole, Role, RolePermission, Permission>, IEntityTypeConfiguration<TwoFactorAuthType>
{
    protected override string Schema => IdentityTableNames.Schemas.Identity;
    protected override string Table => IdentityTableNames.TwoFactorAuthTypes;

    // Re-implements the interface so the code-delivery methods are seeded next to GM.Identity's "None" (id 0).
    // Their ids are the TwoFactorMethod values the login flow maps to a delivery channel.
    public new void Configure(EntityTypeBuilder<TwoFactorAuthType> builder)
    {
        base.Configure(builder);

        var seededAt = new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            Method(TwoFactorMethod.Email, "Email"),
            Method(TwoFactorMethod.Sms, "SMS"),
            Method(TwoFactorMethod.WhatsApp, "WhatsApp"));

        object Method(TwoFactorMethod method, string displayName) => new
        {
            Id = (int)method,
            Name = method.ToString(),
            DisplayName = displayName,
            IsActive = true,
            IsDeleted = false,
            IsHidden = false,
            CreatedAt = seededAt,
            UpdatedAt = seededAt,
        };
    }
}