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
}
