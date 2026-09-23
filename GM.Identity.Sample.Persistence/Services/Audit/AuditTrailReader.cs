using GM.Identity.Sample.Application.Infrastructure.Services.Audit;
using GM.Identity.Sample.Persistence.Context;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Persistence.Services.Audit;

/// <summary>
/// Reads the durable domain-event log from <see cref="ApplicationDbContext"/> (the event store lives on the
/// GM.EntityFramework base context) for the application's internal lookups. The full history is not exposed
/// through the API; it is queried directly in the domain-event store table.
/// </summary>
public class AuditTrailReader(ApplicationDbContext dbContext) : IAuditTrailReader
{
    public async Task<DateTime?> GetLatestEventOccurredOnAsync(
        string aggregateType, string aggregateId, string eventType, CancellationToken cancellationToken)
    {
        var occurredOn = await dbContext.DomainEvents
            .AsNoTracking()
            .Where(x => x.AggregateType == aggregateType && x.AggregateId == aggregateId && x.EventType == eventType)
            .OrderByDescending(x => x.OccurredOn)
            .Select(x => (DateTime?)x.OccurredOn)
            .FirstOrDefaultAsync(cancellationToken);

        return occurredOn;
    }
}
