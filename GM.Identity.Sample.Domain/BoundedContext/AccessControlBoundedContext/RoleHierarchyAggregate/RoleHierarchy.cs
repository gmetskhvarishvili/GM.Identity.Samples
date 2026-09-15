using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleHierarchyAggregate;

/// <summary>
/// A parent edge in the role hierarchy: <see cref="RoleId"/> inherits all permissions of <see cref="ParentRoleId"/>
/// (transitively). Kept as its own aggregate so a role can be re-parented without touching the role itself.
/// A role has at most one parent.
/// </summary>
public class RoleHierarchy : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private RoleHierarchy() // EF Core materialization
    {
    }

    private RoleHierarchy(Guid roleId, Guid parentRoleId)
    {
        Id = Guid.NewGuid();
        RoleId = roleId;
        ParentRoleId = parentRoleId;
    }

    public static RoleHierarchy Create(Guid roleId, Guid parentRoleId) => new(roleId, parentRoleId);

    public Guid RoleId { get; private set; }
    public Guid ParentRoleId { get; private set; }
}
