using GM.EntityFramework.Domain.Events;
using GM.Identity.Domain.AccessControl.ClientScopeAggregate.Events;
using GM.Scheduling;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// On a revoked client-scope, fires <see cref="ClientScopeCacheEvictionJob"/> to evict it from the scope cache.
/// The command no longer writes the cache inline. Reacts to <see cref="GMClientScopeDeletedDomainEvent"/>.
/// </summary>
public sealed class ClientScopeDeletedCacheEvictionHandler(IJobScheduler jobScheduler)
    : IDomainEventHandler<GMClientScopeDeletedDomainEvent>
{
    public Task HandleAsync(GMClientScopeDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => jobScheduler.TriggerNowAsync(
            ClientScopeCacheEvictionJob.JobName,
            new Dictionary<string, string>
            {
                [ClientScopeCacheEvictionJob.ClientIdKey] = domainEvent.ClientId.ToString(),
                [ClientScopeCacheEvictionJob.ScopeIdKey] = domainEvent.ScopeId.ToString(),
            },
            cancellationToken);
}
