using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Persistence.Context;
using GM.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Maintenance;

/// <summary>
/// Enforces the audit-log data-retention policy: deletes domain-event records older than the configured
/// retention window. Runs daily; distributed-lock-guarded by GM.Scheduling; discovered via ScanAssemblies.
/// </summary>
[ScheduledJob("audit-retention-purge", Cron = "0 30 3 * * ?")]
public sealed class AuditRetentionPurgeJob(
    ApplicationDbContext dbContext,
    IOptions<RetentionOptions> options) : IScheduledJob
{
    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var days = options.Value.AuditRetentionDays;
        if (days <= 0)
            return JobExecutionResult.Success(0, "Audit retention disabled (AuditRetentionDays <= 0).");

        var cutoff = DateTime.UtcNow.AddDays(-days);
        var deleted = await dbContext.DomainEvents
            .Where(x => x.OccurredOn < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        return JobExecutionResult.Success(deleted, $"Purged {deleted} audit events older than {days} days.");
    }
}
