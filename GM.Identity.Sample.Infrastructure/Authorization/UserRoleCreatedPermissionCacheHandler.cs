using GM.EntityFramework.Domain.Events;
using GM.Identity.Authorization;
using GM.Identity.Domain.AccessControl.UserRoleAggregate.Events;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Write-throughs a newly granted user-role into the Redis RBAC projection. Invoked by the domain-event
/// dispatcher once the <see cref="GMUserRoleCreatedDomainEvent"/>'s aggregate is persisted, replacing the
/// inline cache write the command handler used to do; the periodic <see cref="PermissionCacheReconciliationJob"/>
/// stays the backstop that self-heals any missed write.
/// </summary>
public sealed class UserRoleCreatedPermissionCacheHandler(IPermissionCache cache)
    : IDomainEventHandler<GMUserRoleCreatedDomainEvent>
{
    public Task HandleAsync(GMUserRoleCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => cache.AddUserRoleAsync(domainEvent.UserId, domainEvent.RoleId, cancellationToken);
}
