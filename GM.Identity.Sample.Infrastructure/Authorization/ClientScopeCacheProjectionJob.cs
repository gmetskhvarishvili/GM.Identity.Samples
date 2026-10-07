using GM.Identity.Authorization;
using GM.Identity.Domain.AccessControl.ClientScopeAggregate.Events;
using GM.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Projects a newly granted client-scope into the Redis scope cache. Trigger-only: fired by the
/// <see cref="GMClientScopeCreatedDomainEvent"/> handler with the client and scope ids. Own DI scope, retries.
/// </summary>
[ScheduledJob(JobName, MaxRetries = 3)]
public sealed class ClientScopeCacheProjectionJob(IScopeCache scopeCache) : IScheduledJob
{
    public const string JobName = "client-scope-cache-project";
    public const string ClientIdKey = "clientId";
    public const string ScopeIdKey = "scopeId";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var clientId = Guid.Parse(context.Data[ClientIdKey]);
        var scopeId = Guid.Parse(context.Data[ScopeIdKey]);

        await scopeCache.AddClientScopeAsync(clientId, scopeId, cancellationToken);

        return JobExecutionResult.Success(1, $"Projected scope {scopeId} for client {clientId} into the scope cache.");
    }
}
