using GM.EntityFramework.Domain.Events;
using GM.Identity.Domain.AccessControl.ClientScopeAggregate.Events;
using GM.Scheduling;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// On a new client-scope grant, fires <see cref="ClientScopeCacheProjectionJob"/> to project it into the scope
/// cache. The command no longer writes the cache inline. Reacts to <see cref="GMClientScopeCreatedDomainEvent"/>.
/// </summary>
public sealed class ClientScopeCreatedCacheProjectionHandler(IJobScheduler jobScheduler)
    : IDomainEventHandler<GMClientScopeCreatedDomainEvent>
{
    public Task HandleAsync(GMClientScopeCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => jobScheduler.TriggerNowAsync(
            ClientScopeCacheProjectionJob.JobName,
            new Dictionary<string, string>
            {
                [ClientScopeCacheProjectionJob.ClientIdKey] = domainEvent.ClientId.ToString(),
                [ClientScopeCacheProjectionJob.ScopeIdKey] = domainEvent.ScopeId.ToString(),
            },
            cancellationToken);
}
