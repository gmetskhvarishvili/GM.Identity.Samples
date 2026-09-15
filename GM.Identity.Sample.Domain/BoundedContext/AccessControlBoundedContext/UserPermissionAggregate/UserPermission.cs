using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserPermissionAggregate;

/// <summary>
/// A permission granted directly to a user, in addition to any it inherits through roles. Projected into the
/// Redis authorization cache under the user's own id as a synthetic "self-role", so <c>[HasPermission]</c>
/// checks honor it with no change to the resolution path.
/// </summary>
public class UserPermission : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private UserPermission() // EF Core materialization
    {
    }

    private UserPermission(Guid userId, Guid permissionId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        PermissionId = permissionId;
    }

    public static UserPermission Create(Guid userId, Guid permissionId) => new(userId, permissionId);

    public Guid UserId { get; private set; }
    public Guid PermissionId { get; private set; }
}
