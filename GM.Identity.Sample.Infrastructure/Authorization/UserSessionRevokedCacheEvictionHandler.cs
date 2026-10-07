using GM.EntityFramework.Domain.Events;
using GM.Identity.Domain.Authorization.UserSessionAggregate.Events;
using GM.Scheduling;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// On a revoked user session, fires <see cref="UserSessionCacheEvictionJob"/> once to evict it from the Redis
/// session cache. The command no longer touches the cache inline; the removal is deferred to the job (own DI
/// scope, retries). Reacts to the library-raised <see cref="GMUserSessionRevokedDomainEvent"/> (raised by the
/// GMUserSession <c>Revoke</c>).
/// </summary>
public sealed class UserSessionRevokedCacheEvictionHandler(IJobScheduler jobScheduler)
    : IDomainEventHandler<GMUserSessionRevokedDomainEvent>
{
    public Task HandleAsync(GMUserSessionRevokedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => jobScheduler.TriggerNowAsync(
            UserSessionCacheEvictionJob.JobName,
            new Dictionary<string, string>
            {
                [UserSessionCacheEvictionJob.TokenHashKey] = domainEvent.TokenHash,
            },
            cancellationToken);
}
