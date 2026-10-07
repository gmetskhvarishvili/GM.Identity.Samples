using GM.Identity.Authorization;
using GM.Identity.Domain.AccessControl.ScopeOperationAggregate.Events;
using GM.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Evicts a revoked scope-operation from the Redis scope cache. Trigger-only: fired by the
/// <see cref="GMScopeOperationDeletedDomainEvent"/> handler with the scope and operation ids. Own DI scope, retries.
/// </summary>
[ScheduledJob(JobName, MaxRetries = 3)]
public sealed class ScopeOperationCacheEvictionJob(IScopeCache scopeCache) : IScheduledJob
{
    public const string JobName = "scope-operation-cache-evict";
    public const string ScopeIdKey = "scopeId";
    public const string OperationIdKey = "operationId";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var scopeId = Guid.Parse(context.Data[ScopeIdKey]);
        var operationId = Guid.Parse(context.Data[OperationIdKey]);

        await scopeCache.RemoveScopeOperationAsync(scopeId, operationId, cancellationToken);

        return JobExecutionResult.Success(1, $"Evicted operation {operationId} for scope {scopeId} from the scope cache.");
    }
}
