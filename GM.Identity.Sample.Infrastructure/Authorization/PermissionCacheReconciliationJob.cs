using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Scheduling;

using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Periodically rebuilds the Redis RBAC projection from the database (the source of truth), so the cache
/// self-heals from any missed write-through or manual DB change. Each fire is guarded by a distributed
/// lock (by GM.Scheduling), so only one instance reconciles at a time. Runs every 10 minutes.
/// </summary>
[ScheduledJob("permission-cache-reconcile", Cron = "0 0/10 * * * ?")]
public sealed class PermissionCacheReconciliationJob(
    IUnitOfWork unitOfWork,
    IPermissionCache cache) : IScheduledJob
{
    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        // Desired state from the DB (only live rows).
        var userRoles = (await unitOfWork.UserRoleRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden);
        var roleIdsByUser = userRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleId).ToHashSet());

        var desiredUserRoles = roleIdsByUser
            .ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<Guid>)kv.Value.ToList());

        // Each role maps to its own permissions.
        var desiredRolePermissions = (await unitOfWork.RolePermissionRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden)
            .GroupBy(x => x.RoleId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Guid>)g.Select(x => x.PermissionId).Distinct().ToList());

        // Overwrite each desired key (atomic per key).
        foreach (var (userId, roleIds) in desiredUserRoles)
            await cache.ReplaceUserRolesAsync(userId, roleIds, cancellationToken);
        foreach (var (roleId, permissionIds) in desiredRolePermissions)
            await cache.ReplaceRolePermissionsAsync(roleId, permissionIds, cancellationToken);

        // Drop keys that no longer have a DB counterpart (deleted users/roles).
        var orphanUsers = 0;
        foreach (var cachedUserId in await cache.GetCachedUserIdsAsync(cancellationToken))
            if (!desiredUserRoles.ContainsKey(cachedUserId))
            {
                await cache.RemoveUserAsync(cachedUserId, cancellationToken);
                orphanUsers++;
            }

        var orphanRoles = 0;
        foreach (var cachedRoleId in await cache.GetCachedRoleIdsAsync(cancellationToken))
            if (!desiredRolePermissions.ContainsKey(cachedRoleId))
            {
                await cache.RemoveRoleAsync(cachedRoleId, cancellationToken);
                orphanRoles++;
            }

        var processed = desiredUserRoles.Count + desiredRolePermissions.Count;
        return JobExecutionResult.Success(
            processed,
            $"Reconciled {desiredUserRoles.Count} users, {desiredRolePermissions.Count} roles; " +
            $"pruned {orphanUsers} user + {orphanRoles} role orphans.");
    }
}
