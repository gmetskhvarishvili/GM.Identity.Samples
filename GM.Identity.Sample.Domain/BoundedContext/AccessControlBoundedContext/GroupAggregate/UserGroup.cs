using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.GroupAggregate;

/// <summary>A user's membership in a group; through it the user inherits the group's roles.</summary>
public class UserGroup : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private UserGroup() { } // EF Core materialization

    private UserGroup(Guid userId, Guid groupId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        GroupId = groupId;
    }

    public static UserGroup Create(Guid userId, Guid groupId) => new(userId, groupId);

    public Guid UserId { get; private set; }
    public Guid GroupId { get; private set; }
}
