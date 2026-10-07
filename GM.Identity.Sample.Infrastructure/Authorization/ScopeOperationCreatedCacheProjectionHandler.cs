using GM.EntityFramework.Domain.Events;
using GM.Identity.Domain.AccessControl.ScopeOperationAggregate.Events;
using GM.Scheduling;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// On a new scope-operation grant, fires <see cref="ScopeOperationCacheProjectionJob"/> to project it into the
/// scope cache. The command no longer writes the cache inline. Reacts to <see cref="GMScopeOperationCreatedDomainEvent"/>.
/// </summary>
public sealed class ScopeOperationCreatedCacheProjectionHandler(IJobScheduler jobScheduler)
    : IDomainEventHandler<GMScopeOperationCreatedDomainEvent>
{
    public Task HandleAsync(GMScopeOperationCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => jobScheduler.TriggerNowAsync(
            ScopeOperationCacheProjectionJob.JobName,
            new Dictionary<string, string>
            {
                [ScopeOperationCacheProjectionJob.ScopeIdKey] = domainEvent.ScopeId.ToString(),
                [ScopeOperationCacheProjectionJob.OperationIdKey] = domainEvent.OperationId.ToString(),
            },
            cancellationToken);
}
