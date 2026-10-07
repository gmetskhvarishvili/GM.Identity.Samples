using GM.Identity.Authorization;
using GM.Identity.Domain.AccessControl.RolePermissionAggregate.Events;
using GM.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Projects a newly granted role-permission into the Redis RBAC projection. Trigger-only (no cron): the
/// <see cref="GMRolePermissionCreatedDomainEvent"/> handler fires it via <see cref="IJobScheduler.TriggerNowAsync"/>,
/// passing the role and permission ids in the job data. Runs in its own DI scope with retries; the periodic
/// <see cref="PermissionCacheReconciliationJob"/> is the backstop for any write that still fails.
/// </summary>
[ScheduledJob(JobName, MaxRetries = 3)]
public sealed class RolePermissionCacheProjectionJob(IPermissionCache cache) : IScheduledJob
{
    public const string JobName = "role-permission-cache-project";
    public const string RoleIdKey = "roleId";
    public const string PermissionIdKey = "permissionId";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var roleId = Guid.Parse(context.Data[RoleIdKey]);
        var permissionId = Guid.Parse(context.Data[PermissionIdKey]);

        await cache.AddRolePermissionAsync(roleId, permissionId, cancellationToken);

        return JobExecutionResult.Success(1, $"Projected permission {permissionId} for role {roleId} into the RBAC cache.");
    }
}
