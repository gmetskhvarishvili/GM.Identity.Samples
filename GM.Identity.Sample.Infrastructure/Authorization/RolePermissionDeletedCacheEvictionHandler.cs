using GM.EntityFramework.Domain.Events;
using GM.Identity.Domain.AccessControl.RolePermissionAggregate.Events;
using GM.Scheduling;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// On a revoked role-permission grant, fires <see cref="RolePermissionCacheEvictionJob"/> once to evict it from
/// the Redis RBAC projection. The command no longer touches the cache inline; the removal is deferred to the job
/// (own DI scope, retries), with the reconciliation job as the backstop. Reacts to the library-raised
/// <see cref="GMRolePermissionDeletedDomainEvent"/> (raised by the GMRolePermission <c>SoftRemove</c>).
/// </summary>
public sealed class RolePermissionDeletedCacheEvictionHandler(IJobScheduler jobScheduler)
    : IDomainEventHandler<GMRolePermissionDeletedDomainEvent>
{
    public Task HandleAsync(GMRolePermissionDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => jobScheduler.TriggerNowAsync(
            RolePermissionCacheEvictionJob.JobName,
            new Dictionary<string, string>
            {
                [RolePermissionCacheEvictionJob.RoleIdKey] = domainEvent.RoleId.ToString(),
                [RolePermissionCacheEvictionJob.PermissionIdKey] = domainEvent.PermissionId.ToString(),
            },
            cancellationToken);
}
