using GM.Identity.Authorization;
using GM.Identity.Domain.AccessControl.OperationAggregate.Events;
using GM.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Registers a newly created operation's name→id mapping in the Redis scope cache. Trigger-only: fired by the
/// <see cref="GMOperationCreatedDomainEvent"/> handler with the operation name and id. Own DI scope, retries.
/// </summary>
[ScheduledJob(JobName, MaxRetries = 3)]
public sealed class OperationScopeCacheProjectionJob(IScopeCache scopeCache) : IScheduledJob
{
    public const string JobName = "operation-scope-cache-project";
    public const string NameKey = "name";
    public const string OperationIdKey = "operationId";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var name = context.Data[NameKey];
        var operationId = Guid.Parse(context.Data[OperationIdKey]);

        await scopeCache.SetOperationIdAsync(name, operationId, cancellationToken);

        return JobExecutionResult.Success(1, $"Registered operation '{name}' ({operationId}) in the scope cache.");
    }
}
