using GM.Identity.Sample.Application.Infrastructure.Services.Audit;
using GM.Identity.Sample.Persistence.Context;
using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Persistence.Services.Audit;

/// <summary>
/// Reads the durable domain-event log from <see cref="ApplicationDbContext"/> (the event store lives on the
/// GM.EntityFramework base context). The <c>(AggregateType, AggregateId, OccurredOn)</c> index makes the
/// per-aggregate history an indexed scan.
/// </summary>
public class AuditTrailReader(ApplicationDbContext dbContext) : IAuditTrailReader
{
    public async Task<(IReadOnlyList<AuditTrailEntry> Items, int TotalCount)> GetForAggregateAsync(
        string aggregateType, string aggregateId, int skip, int take, CancellationToken cancellationToken)
    {
        var query = dbContext.DomainEvents
            .AsNoTracking()
            .Where(x => x.AggregateType == aggregateType && x.AggregateId == aggregateId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.OccurredOn)
            .Skip(skip)
            .Take(take)
            .Select(x => new AuditTrailEntry
            {
                Id = x.Id,
                EventType = x.EventType,
                OccurredOn = x.OccurredOn,
                UserId = x.UserId,
                ClientId = x.ClientId,
                TenantId = x.TenantId,
                SessionId = x.SessionId,
                IpAddress = x.IpAddress,
                CorrelationId = x.CorrelationId,
                Payload = x.Payload,
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<AuditTrailEntry> Items, int TotalCount)> SearchAsync(
        AuditTrailSearch search, int skip, int take, CancellationToken cancellationToken)
    {
        var query = dbContext.DomainEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search.AggregateType))
            query = query.Where(x => x.AggregateType == search.AggregateType);
        if (search.ActorUserId is { } actor)
            query = query.Where(x => x.UserId == actor);
        if (!string.IsNullOrWhiteSpace(search.EventType))
            query = query.Where(x => x.EventType == search.EventType);
        if (search.OccurredFrom is { } from)
            query = query.Where(x => x.OccurredOn >= from);
        if (search.OccurredTo is { } to)
            query = query.Where(x => x.OccurredOn <= to);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.OccurredOn)
            .Skip(skip)
            .Take(take)
            .Select(x => new AuditTrailEntry
            {
                Id = x.Id,
                EventType = x.EventType,
                OccurredOn = x.OccurredOn,
                UserId = x.UserId,
                ClientId = x.ClientId,
                TenantId = x.TenantId,
                SessionId = x.SessionId,
                IpAddress = x.IpAddress,
                CorrelationId = x.CorrelationId,
                Payload = x.Payload,
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<System.DateTime?> GetLatestEventOccurredOnAsync(
        string aggregateType, string aggregateId, string eventType, CancellationToken cancellationToken)
    {
        var occurredOn = await dbContext.DomainEvents
            .AsNoTracking()
            .Where(x => x.AggregateType == aggregateType && x.AggregateId == aggregateId && x.EventType == eventType)
            .OrderByDescending(x => x.OccurredOn)
            .Select(x => (System.DateTime?)x.OccurredOn)
            .FirstOrDefaultAsync(cancellationToken);

        return occurredOn;
    }
}
