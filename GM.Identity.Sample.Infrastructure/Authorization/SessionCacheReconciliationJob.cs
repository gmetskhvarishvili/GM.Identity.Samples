using GM.Identity.Sample.Application.Common.Authorization;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Scheduling;

using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Periodically rebuilds the Redis session store from the database (the source of truth): every live
/// (active, non-revoked, unexpired) <c>UserSession</c> and <c>ClientSession</c> is written keyed by its
/// token hash, and cached entries with no live DB counterpart are pruned. Self-heals from any missed
/// write-through or a manual DB change. Distributed-lock-guarded per fire by GM.Scheduling. Runs every
/// 10 minutes, offset from the permission-cache job. Discovered automatically via ScanAssemblies.
/// </summary>
[ScheduledJob("session-cache-reconcile", Cron = "0 5/10 * * * ?")]
public sealed class SessionCacheReconciliationJob(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IScheduledJob
{
    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Desired state = live sessions from the DB, keyed by token hash (which is also the Redis key).
        var desired = new Dictionary<string, SessionInfo>();

        var userSessions = (await unitOfWork.UserSessionRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden && !x.IsRevoked && x.ExpiresAt > now)
            .ToList();
        foreach (var s in userSessions)
            desired[s.TokenHash] = new SessionInfo(s.UserId, s.Id, s.ClientId, s.ExpiresAt);

        var clientSessions = (await unitOfWork.ClientSessionRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden && !x.IsRevoked && x.ExpiresAt > now)
            .ToList();
        foreach (var s in clientSessions)
            desired[s.TokenHash] = new SessionInfo(null, s.Id, s.ClientId, s.ExpiresAt);

        foreach (var (tokenHash, info) in desired)
            await sessionCache.SetAsync(tokenHash, info, cancellationToken);

        var orphans = 0;
        foreach (var cachedHash in await sessionCache.GetCachedTokenHashesAsync(cancellationToken))
            if (!desired.ContainsKey(cachedHash))
            {
                await sessionCache.RemoveAsync(cachedHash, cancellationToken);
                orphans++;
            }

        return JobExecutionResult.Success(
            desired.Count,
            $"Reconciled {userSessions.Count} user + {clientSessions.Count} client sessions; pruned {orphans} orphans.");
    }
}
