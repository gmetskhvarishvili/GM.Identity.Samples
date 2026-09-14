using System.Reflection;
using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using Xunit;

using System.Linq;
using System;
namespace GM.Identity.Sample.Tests;

public class AggregateConstructorTests
{
    // Guard for the "constructor raises Created" design: EF Core materializes via a constructor and
    // prefers a parameterless one. Every aggregate must declare a parameterless constructor so loads go
    // through it (side-effect-free) rather than the identity-assigning, event-raising parameterized one â€”
    // otherwise a phantom created event is raised on every read.
    [Fact]
    public void Every_aggregate_declares_a_parameterless_constructor_for_ef_materialization()
    {
        var aggregates = typeof(Permission).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IAggregateRoot).IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(aggregates);

        var missing = aggregates
            .Where(t => t.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                Type.EmptyTypes,
                modifiers: null) is null)
            .Select(t => t.Name)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"Aggregates without a parameterless constructor (EF would materialize via a side-effecting " +
            $"constructor and raise phantom created events on load): {string.Join(", ", missing)}");
    }
}
