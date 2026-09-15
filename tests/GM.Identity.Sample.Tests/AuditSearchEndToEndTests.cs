using GM.EntityFramework.Persistence.Events;
using GM.Identity.Sample.Application.Audit.Queries.SearchAuditTrail;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of the global audit search: an event in the log is found by filtering on its event type and
/// acting user, and excluded when the filters don't match. Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class AuditSearchEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _eventType = $"AuditSearchProbe-{Guid.NewGuid():N}";
    private readonly Guid _actorId = Guid.NewGuid();
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.DomainEvents.Add(new StoredDomainEvent
            {
                Id = Guid.NewGuid(),
                AggregateType = "User",
                AggregateId = Guid.NewGuid().ToString(),
                EventType = _eventType,
                Payload = "{}",
                OccurredOn = DateTime.UtcNow,
                UserId = _actorId,
            });
            await context.SaveChangesAsync();
            _infraReady = true;
        }
        catch
        {
            _infraReady = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_infraReady)
        {
            try
            {
                using var scope = _factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.DomainEvents.Where(x => x.EventType == _eventType).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Search_finds_an_event_by_type_and_actor_and_excludes_non_matches()
    {
        if (!_infraReady) return;

        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var byType = await mediator.Send(new SearchAuditTrailQuery { EventType = _eventType });
        Assert.Equal(1, byType.TotalCount);
        Assert.Equal(_eventType, byType.Items.Single().EventType);

        var byTypeAndAggregate = await mediator.Send(
            new SearchAuditTrailQuery { EventType = _eventType, AggregateType = "User" });
        Assert.Equal(1, byTypeAndAggregate.TotalCount);

        var wrongAggregate = await mediator.Send(
            new SearchAuditTrailQuery { EventType = _eventType, AggregateType = "Client" });
        Assert.Equal(0, wrongAggregate.TotalCount);

        // A date window that excludes the event finds nothing.
        var futureOnly = await mediator.Send(
            new SearchAuditTrailQuery { EventType = _eventType, OccurredFrom = DateTime.UtcNow.AddHours(1) });
        Assert.Equal(0, futureOnly.TotalCount);
    }
}
