using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.GroupAggregate;

/// <summary>A role granted to a group; every member of the group inherits it.</summary>
public class GroupRole : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private GroupRole() { } // EF Core materialization

    private GroupRole(Guid groupId, Guid roleId)
    {
        Id = Guid.NewGuid();
        GroupId = groupId;
        RoleId = roleId;
    }

    public static GroupRole Create(Guid groupId, Guid roleId) => new(groupId, roleId);

    public Guid GroupId { get; private set; }
    public Guid RoleId { get; private set; }
}
