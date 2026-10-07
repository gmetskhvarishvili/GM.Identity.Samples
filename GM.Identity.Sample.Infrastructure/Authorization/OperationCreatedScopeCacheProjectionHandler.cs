using GM.EntityFramework.Domain.Events;
using GM.Identity.Domain.AccessControl.OperationAggregate.Events;
using GM.Scheduling;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// On a new operation, fires <see cref="OperationScopeCacheProjectionJob"/> to register its name→id mapping in
/// the scope cache. The command no longer writes the cache inline. Reacts to <see cref="GMOperationCreatedDomainEvent"/>.
/// </summary>
public sealed class OperationCreatedScopeCacheProjectionHandler(IJobScheduler jobScheduler)
    : IDomainEventHandler<GMOperationCreatedDomainEvent>
{
    public Task HandleAsync(GMOperationCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => jobScheduler.TriggerNowAsync(
            OperationScopeCacheProjectionJob.JobName,
            new Dictionary<string, string>
            {
                [OperationScopeCacheProjectionJob.NameKey] = domainEvent.Name,
                [OperationScopeCacheProjectionJob.OperationIdKey] = domainEvent.OperationId.ToString(),
            },
            cancellationToken);
}
