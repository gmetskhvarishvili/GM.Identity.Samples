using GM.Identity.Sample.Domain.SeedWork;
using GM.Scheduling;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Maintenance;

/// <summary>
/// Reaps short-lived security artifacts that are no longer usable so the tables don't grow without bound:
/// dead sessions (expired or revoked) and spent/expired authorization codes. A small grace window keeps very
/// recent rows around for diagnosability. Distributed-lock-guarded per fire by GM.Scheduling; discovered
/// automatically via ScanAssemblies.
/// </summary>
[ScheduledJob("expired-artifacts-purge", Cron = "0 20/30 * * * ?")]
public sealed class ExpiredArtifactsPurgeJob(IUnitOfWork unitOfWork) : IScheduledJob
{
    private static readonly TimeSpan Grace = TimeSpan.FromHours(1);

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow - Grace;

        var userSessions = await unitOfWork.UserSessionRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .Where(x => x.ExpiresAt < cutoff || (x.IsRevoked && x.UpdatedAt != null && x.UpdatedAt < cutoff))
            .ExecuteDeleteAsync(cancellationToken);

        var clientSessions = await unitOfWork.ClientSessionRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .Where(x => x.ExpiresAt < cutoff || (x.IsRevoked && x.UpdatedAt != null && x.UpdatedAt < cutoff))
            .ExecuteDeleteAsync(cancellationToken);

        var authCodes = await unitOfWork.AuthorizationCodeRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .Where(x => x.ExpiresAt < cutoff || x.ConsumedAt != null)
            .ExecuteDeleteAsync(cancellationToken);

        var total = userSessions + clientSessions + authCodes;
        return JobExecutionResult.Success(
            total,
            $"Purged {userSessions} user + {clientSessions} client sessions, {authCodes} auth codes.");
    }
}
