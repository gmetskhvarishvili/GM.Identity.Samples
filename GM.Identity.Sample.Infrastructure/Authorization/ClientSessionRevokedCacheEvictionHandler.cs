using GM.EntityFramework.Domain.Events;
using GM.Identity.Domain.Authorization.ClientSessionAggregate.Events;
using GM.Scheduling;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// On a revoked client session, fires <see cref="ClientSessionCacheEvictionJob"/> to evict it from the session
/// cache. The commands no longer touch the cache inline; the removal is deferred to the job (own DI scope,
/// retries). Reacts to the library-raised <see cref="GMClientSessionRevokedDomainEvent"/>.
/// </summary>
public sealed class ClientSessionRevokedCacheEvictionHandler(IJobScheduler jobScheduler)
    : IDomainEventHandler<GMClientSessionRevokedDomainEvent>
{
    public Task HandleAsync(GMClientSessionRevokedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => jobScheduler.TriggerNowAsync(
            ClientSessionCacheEvictionJob.JobName,
            new Dictionary<string, string>
            {
                [ClientSessionCacheEvictionJob.TokenHashKey] = domainEvent.TokenHash,
            },
            cancellationToken);
}
