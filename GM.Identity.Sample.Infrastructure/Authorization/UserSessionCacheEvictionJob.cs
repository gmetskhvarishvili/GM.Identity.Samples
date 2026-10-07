using GM.Identity.Authorization;
using GM.Identity.Domain.Authorization.UserSessionAggregate.Events;
using GM.Scheduling;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Evicts a revoked user session from the Redis session cache (so it can't be used before its TTL expires).
/// Trigger-only (no cron): the <see cref="GMUserSessionRevokedDomainEvent"/> handler fires it via
/// <see cref="IJobScheduler.TriggerNowAsync"/>, passing the session's token hash in the job data. Runs in its
/// own DI scope with retries.
/// </summary>
[ScheduledJob(JobName, MaxRetries = 3)]
public sealed class UserSessionCacheEvictionJob(ISessionCache sessionCache) : IScheduledJob
{
    public const string JobName = "user-session-cache-evict";
    public const string TokenHashKey = "tokenHash";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var tokenHash = context.Data[TokenHashKey];

        await sessionCache.RemoveAsync(tokenHash, cancellationToken);

        return JobExecutionResult.Success(1, "Evicted a revoked user session from the session cache.");
    }
}
