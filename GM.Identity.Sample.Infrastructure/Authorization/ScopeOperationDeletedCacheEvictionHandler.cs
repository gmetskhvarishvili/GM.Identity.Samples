using GM.EntityFramework.Domain.Events;
using GM.Identity.Domain.AccessControl.ScopeOperationAggregate.Events;
using GM.Scheduling;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// On a revoked scope-operation, fires <see cref="ScopeOperationCacheEvictionJob"/> to evict it from the scope
/// cache. The command no longer writes the cache inline. Reacts to <see cref="GMScopeOperationDeletedDomainEvent"/>.
/// </summary>
public sealed class ScopeOperationDeletedCacheEvictionHandler(IJobScheduler jobScheduler)
    : IDomainEventHandler<GMScopeOperationDeletedDomainEvent>
{
    public Task HandleAsync(GMScopeOperationDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => jobScheduler.TriggerNowAsync(
            ScopeOperationCacheEvictionJob.JobName,
            new Dictionary<string, string>
            {
                [ScopeOperationCacheEvictionJob.ScopeIdKey] = domainEvent.ScopeId.ToString(),
                [ScopeOperationCacheEvictionJob.OperationIdKey] = domainEvent.OperationId.ToString(),
            },
            cancellationToken);
}
