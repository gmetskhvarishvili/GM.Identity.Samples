using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.TimeBoundRoleGrantAggregate;

/// <summary>
/// A role assigned to a user only until <see cref="ExpiresAt"/> (temporary/elevated access). It contributes to
/// the user's effective roles while unexpired; once past its expiry it stops granting access and is reaped.
/// </summary>
public class TimeBoundRoleGrant : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private TimeBoundRoleGrant() { } // EF Core materialization

    private TimeBoundRoleGrant(Guid userId, Guid roleId, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RoleId = roleId;
        ExpiresAt = expiresAt;
    }

    public static TimeBoundRoleGrant Create(Guid userId, Guid roleId, DateTime expiresAt) =>
        new(userId, roleId, expiresAt);

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public bool IsExpired(DateTime now) => ExpiresAt <= now;
}
