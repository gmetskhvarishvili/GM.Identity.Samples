using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Recomputes a user's effective role set for the authorization cache: their directly-assigned roles, plus the
/// roles of every group they belong to, plus the synthetic self-role when they hold any direct permission.
/// Shared by the group/permission write-through paths so the cache matches what the reconcile job would build.
/// </summary>
public static class UserRoleProjection
{
    public static async Task RebuildUserAsync(
        IUnitOfWork unitOfWork, IPermissionCache cache, Guid userId, CancellationToken cancellationToken)
    {
        var roleIds = new HashSet<Guid>();

        var directRoles = await unitOfWork.UserRoleRepository
            .Query(false, null).IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);
        roleIds.UnionWith(directRoles);

        var groupIds = await unitOfWork.UserGroupRepository
            .Query(false, null).IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.GroupId)
            .ToListAsync(cancellationToken);
        if (groupIds.Count > 0)
        {
            var groupRoles = await unitOfWork.GroupRoleRepository
                .Query(false, null).IgnoreQueryFilters()
                .Where(x => groupIds.Contains(x.GroupId) && x.IsActive && !x.IsDeleted && !x.IsHidden)
                .Select(x => x.RoleId)
                .ToListAsync(cancellationToken);
            roleIds.UnionWith(groupRoles);
        }

        // Synthetic self-role carries any directly-granted permissions.
        var hasDirectPermissions = await unitOfWork.UserPermissionRepository.ExistsAsync(
            x => x.UserId == userId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (hasDirectPermissions)
            roleIds.Add(userId);

        await cache.ReplaceUserRolesAsync(userId, roleIds.ToList(), cancellationToken);
    }

    /// <summary>Rebuilds every member of a group (used when the group's roles change).</summary>
    public static async Task RebuildGroupMembersAsync(
        IUnitOfWork unitOfWork, IPermissionCache cache, Guid groupId, CancellationToken cancellationToken)
    {
        var memberIds = await unitOfWork.UserGroupRepository
            .Query(false, null).IgnoreQueryFilters()
            .Where(x => x.GroupId == groupId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var userId in memberIds)
            await RebuildUserAsync(unitOfWork, cache, userId, cancellationToken);
    }
}
