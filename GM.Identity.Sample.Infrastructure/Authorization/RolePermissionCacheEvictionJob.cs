using GM.Identity.Authorization;
using GM.Identity.Domain.AccessControl.RolePermissionAggregate.Events;
using GM.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Evicts a revoked role-permission from the Redis RBAC projection. Trigger-only (no cron): the
/// <see cref="GMRolePermissionDeletedDomainEvent"/> handler fires it via <see cref="IJobScheduler.TriggerNowAsync"/>,
/// passing the role and permission ids in the job data. Runs in its own DI scope with retries; the periodic
/// <see cref="PermissionCacheReconciliationJob"/> is the backstop for any eviction that still fails.
/// </summary>
[ScheduledJob(JobName, MaxRetries = 3)]
public sealed class RolePermissionCacheEvictionJob(IPermissionCache cache) : IScheduledJob
{
    public const string JobName = "role-permission-cache-evict";
    public const string RoleIdKey = "roleId";
    public const string PermissionIdKey = "permissionId";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var roleId = Guid.Parse(context.Data[RoleIdKey]);
        var permissionId = Guid.Parse(context.Data[PermissionIdKey]);

        await cache.RemoveRolePermissionAsync(roleId, permissionId, cancellationToken);

        return JobExecutionResult.Success(1, $"Evicted permission {permissionId} for role {roleId} from the RBAC cache.");
    }
}
