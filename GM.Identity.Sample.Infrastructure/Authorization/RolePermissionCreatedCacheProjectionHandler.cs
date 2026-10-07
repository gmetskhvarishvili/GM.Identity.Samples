using GM.EntityFramework.Domain.Events;
using GM.Identity.Domain.AccessControl.RolePermissionAggregate.Events;
using GM.Scheduling;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// On a new role-permission grant, fires <see cref="RolePermissionCacheProjectionJob"/> once to project it into
/// the Redis RBAC projection. The command no longer touches the cache inline; the write is deferred to the job
/// (own DI scope, retries), with the reconciliation job as the backstop. Reacts to the library-raised
/// <see cref="GMRolePermissionCreatedDomainEvent"/> (raised by the GMRolePermission constructor).
/// </summary>
public sealed class RolePermissionCreatedCacheProjectionHandler(IJobScheduler jobScheduler)
    : IDomainEventHandler<GMRolePermissionCreatedDomainEvent>
{
    public Task HandleAsync(GMRolePermissionCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => jobScheduler.TriggerNowAsync(
            RolePermissionCacheProjectionJob.JobName,
            new Dictionary<string, string>
            {
                [RolePermissionCacheProjectionJob.RoleIdKey] = domainEvent.RoleId.ToString(),
                [RolePermissionCacheProjectionJob.PermissionIdKey] = domainEvent.PermissionId.ToString(),
            },
            cancellationToken);
}
