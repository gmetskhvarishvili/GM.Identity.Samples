using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.GroupAggregate;

/// <summary>
/// A group (team) that bundles roles. Users placed in the group inherit its roles, so access can be managed by
/// team membership instead of assigning each role to each user.
/// </summary>
public class Group : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private Group() // EF Core materialization
    {
    }

    private Group(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    public static Group Create(string name) => new(name);

    public string Name { get; private set; } = null!;

    public void Rename(string name) => Name = name;
}
