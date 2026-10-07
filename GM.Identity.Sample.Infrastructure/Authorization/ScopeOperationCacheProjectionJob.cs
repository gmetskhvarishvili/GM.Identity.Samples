using GM.Identity.Authorization;
using GM.Identity.Domain.AccessControl.ScopeOperationAggregate.Events;
using GM.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Projects a newly granted scope-operation into the Redis scope cache. Trigger-only: fired by the
/// <see cref="GMScopeOperationCreatedDomainEvent"/> handler with the scope and operation ids. Own DI scope, retries.
/// </summary>
[ScheduledJob(JobName, MaxRetries = 3)]
public sealed class ScopeOperationCacheProjectionJob(IScopeCache scopeCache) : IScheduledJob
{
    public const string JobName = "scope-operation-cache-project";
    public const string ScopeIdKey = "scopeId";
    public const string OperationIdKey = "operationId";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var scopeId = Guid.Parse(context.Data[ScopeIdKey]);
        var operationId = Guid.Parse(context.Data[OperationIdKey]);

        await scopeCache.AddScopeOperationAsync(scopeId, operationId, cancellationToken);

        return JobExecutionResult.Success(1, $"Projected operation {operationId} for scope {scopeId} into the scope cache.");
    }
}
