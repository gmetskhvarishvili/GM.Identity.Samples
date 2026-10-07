using System.Reflection;
using GM.Identity.Domain.AccessControl.PermissionAggregate.Events;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using Xunit;

using System;
namespace GM.Identity.Sample.Tests;

public class PermissionAggregateTests
{
    [Fact]
    public void Create_RaisesASingleCreatedEvent_WithAnAssignedId()
    {
        var permission = Permission.Create("domain", "description");

        Assert.NotEqual(Guid.Empty, permission.Id);
        var created = Assert.Single(permission.DomainEvents);
        var createdEvent = Assert.IsType<GMPermissionCreatedDomainEvent>(created);
        Assert.Equal(permission.Id, createdEvent.PermissionId);
    }

    // Regression: EF Core materializes an entity through a constructor. Because every concrete entity
    // declares a parameterless constructor, EF prefers it â€” and it is side-effect-free, so loading an
    // existing row raises no domain events. (The identity-assigning, event-raising constructor is the
    // parameterized one, reached only through the Create factory.)
    [Fact]
    public void Materializing_ThroughTheParameterlessConstructor_RaisesNoEvents()
    {
        var permission = MaterializeAsEfWould();

        Assert.Empty(permission.DomainEvents);
    }

    // The exact scenario reported: load an existing permission, then update it. Only the update event
    // must appear â€” no phantom created event from the load.
    [Fact]
    public void Update_AfterMaterialization_RaisesOnlyTheUpdatedEvent()
    {
        var permission = MaterializeAsEfWould();

        permission.Update("domain 44", "description");

        var raised = Assert.Single(permission.DomainEvents);
        Assert.IsType<GMPermissionUpdatedDomainEvent>(raised);
    }

    // Invokes the parameterless constructor the way EF Core's materializer does, bypassing the factory.
    private static Permission MaterializeAsEfWould()
    {
        var constructor = typeof(Permission).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        Assert.NotNull(constructor);
        return (Permission)constructor.Invoke([]);
    }
}
