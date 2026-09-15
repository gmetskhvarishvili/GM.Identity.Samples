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

        // Group membership contributes the group's roles to each member.
        var groupRoles = (await unitOfWork.GroupRoleRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden)
            .GroupBy(x => x.GroupId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleId).ToList());
        var userGroups = (await unitOfWork.UserGroupRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden);
        foreach (var membership in userGroups)
        {
            if (!groupRoles.TryGetValue(membership.GroupId, out var rolesOfGroup)) continue;
            if (!roleIdsByUser.TryGetValue(membership.UserId, out var set))
                roleIdsByUser[membership.UserId] = set = new HashSet<Guid>();
            set.UnionWith(rolesOfGroup);
        }

        // Time-bound role grants that haven't expired contribute too.
        var now = DateTime.UtcNow;
        var temporaryGrants = (await unitOfWork.TimeBoundRoleGrantRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.ExpiresAt > now && x.IsActive && !x.IsDeleted && !x.IsHidden);
        foreach (var grant in temporaryGrants)
        {
            if (!roleIdsByUser.TryGetValue(grant.UserId, out var set))
                roleIdsByUser[grant.UserId] = set = new HashSet<Guid>();
            set.Add(grant.RoleId);
        }

        var desiredUserRoles = roleIdsByUser
            .ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<Guid>)kv.Value.ToList());

        // Role permissions expanded along the hierarchy (a role inherits its ancestors' permissions).
        var ownRolePermissions = (await unitOfWork.RolePermissionRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden)
            .GroupBy(x => x.RoleId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Guid>)g.Select(x => x.PermissionId).Distinct().ToList());
        var parentByRole = (await unitOfWork.RoleHierarchyRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden)
            .GroupBy(x => x.RoleId)
            .ToDictionary(g => g.Key, g => g.First().ParentRoleId);
        var desiredRolePermissions = RoleHierarchyExpansion.ComputeEffective(ownRolePermissions, parentByRole)
            .ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<Guid>)kv.Value.ToList());

        // Direct user permissions are projected under the user's own id as a synthetic "self-role": add that
        // id to the user's role set and give it the directly-granted permissions.
        var userPermissions = (await unitOfWork.UserPermissionRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden)
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PermissionId).Distinct().ToList());

        foreach (var (userId, permissionIds) in userPermissions)
        {
            var roles = desiredUserRoles.TryGetValue(userId, out var existing)
                ? new List<Guid>(existing)
                : new List<Guid>();
            if (!roles.Contains(userId))
                roles.Add(userId);
            desiredUserRoles[userId] = roles;
            desiredRolePermissions[userId] = permissionIds;
        }

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
