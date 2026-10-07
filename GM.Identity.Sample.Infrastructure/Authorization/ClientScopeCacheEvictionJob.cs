using GM.Identity.Authorization;
using GM.Identity.Domain.AccessControl.ClientScopeAggregate.Events;
using GM.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Evicts a revoked client-scope from the Redis scope cache. Trigger-only: fired by the
/// <see cref="GMClientScopeDeletedDomainEvent"/> handler with the client and scope ids. Own DI scope, retries.
/// </summary>
[ScheduledJob(JobName, MaxRetries = 3)]
public sealed class ClientScopeCacheEvictionJob(IScopeCache scopeCache) : IScheduledJob
{
    public const string JobName = "client-scope-cache-evict";
    public const string ClientIdKey = "clientId";
    public const string ScopeIdKey = "scopeId";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var clientId = Guid.Parse(context.Data[ClientIdKey]);
        var scopeId = Guid.Parse(context.Data[ScopeIdKey]);

        await scopeCache.RemoveClientScopeAsync(clientId, scopeId, cancellationToken);

        return JobExecutionResult.Success(1, $"Evicted scope {scopeId} for client {clientId} from the scope cache.");
    }
}
