using GM.Identity.Authorization;
using GM.Identity.Domain.AccessControl.UserRoleAggregate.Events;
using GM.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Evicts a single revoked user-role from the Redis RBAC projection. Trigger-only (no cron): the
/// <see cref="GMUserRoleDeletedDomainEvent"/> handler fires it via <see cref="IJobScheduler.TriggerNowAsync"/>,
/// passing the user and role ids in the job data. Runs in its own DI scope with retries; the periodic
/// <see cref="PermissionCacheReconciliationJob"/> is the backstop for any eviction that still fails.
/// </summary>
[ScheduledJob(JobName, MaxRetries = 3)]
public sealed class UserRoleCacheEvictionJob(IPermissionCache cache) : IScheduledJob
{
    public const string JobName = "user-role-cache-evict";
    public const string UserIdKey = "userId";
    public const string RoleIdKey = "roleId";

    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(context.Data[UserIdKey]);
        var roleId = Guid.Parse(context.Data[RoleIdKey]);

        await cache.RemoveUserRoleAsync(userId, roleId, cancellationToken);

        return JobExecutionResult.Success(1, $"Evicted role {roleId} from user {userId} in the RBAC cache.");
    }
}
