using GM.Identity.Authorization;
using GM.Identity.Domain.Authorization.ClientSessionAggregate.Events;
using GM.Scheduling;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Evicts a revoked client session from the Redis session cache. Trigger-only: the
/// <see cref="GMClientSessionRevokedDomainEvent"/> handler fires it via <see cref="IJobScheduler.TriggerNowAsync"/>,
/// passing the session's token hash in the job data. Runs in its own DI scope with retries.
/// </summary>
[ScheduledJob(JobName, MaxRetries = 3)]
public sealed class ClientSessionCacheEvictionJob(ISessionCache sessionCache) : IScheduledJob
{
    public const string JobName = "client-session-cache-evict";
    public const string TokenHashKey = "tokenHash";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var tokenHash = context.Data[TokenHashKey];

        await sessionCache.RemoveAsync(tokenHash, cancellationToken);

        return JobExecutionResult.Success(1, "Evicted a revoked client session from the session cache.");
    }
}
