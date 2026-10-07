using GM.EntityFramework.Domain.Events;
using GM.Identity.Domain.AccessControl.UserRoleAggregate.Events;
using GM.Scheduling;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// On a revoked role grant, fires <see cref="UserRoleCacheEvictionJob"/> once (right now) to evict the entry
/// from the Redis RBAC projection. The command no longer touches the cache inline; the removal is deferred to
/// the job (own DI scope, retries), with the reconciliation job as the backstop.
/// </summary>
public sealed class UserRoleDeletedCacheEvictionHandler(IJobScheduler jobScheduler)
    : IDomainEventHandler<GMUserRoleDeletedDomainEvent>
{
    public Task HandleAsync(GMUserRoleDeletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => jobScheduler.TriggerNowAsync(
            UserRoleCacheEvictionJob.JobName,
            new Dictionary<string, string>
            {
                [UserRoleCacheEvictionJob.UserIdKey] = domainEvent.UserId.ToString(),
                [UserRoleCacheEvictionJob.RoleIdKey] = domainEvent.RoleId.ToString(),
            },
            cancellationToken);
}
