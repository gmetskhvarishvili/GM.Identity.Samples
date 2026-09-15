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
/// Recomputes the effective role→permission projection (own permissions expanded along the hierarchy) and
/// writes it to the authorization cache. Shared by the write-through path (on a hierarchy change) and the
/// reconcile job, so both produce the same result.
/// </summary>
public static class RoleHierarchyProjection
{
    public static async Task ReprojectAsync(
        IUnitOfWork unitOfWork, IPermissionCache cache, CancellationToken cancellationToken)
    {
        var ownPermissions = (await unitOfWork.RolePermissionRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden)
            .GroupBy(x => x.RoleId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Guid>)g.Select(x => x.PermissionId).Distinct().ToList());

        var parentByRole = (await unitOfWork.RoleHierarchyRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden)
            .GroupBy(x => x.RoleId)
            .ToDictionary(g => g.Key, g => g.First().ParentRoleId);

        var effective = RoleHierarchyExpansion.ComputeEffective(ownPermissions, parentByRole);

        foreach (var (roleId, permissionIds) in effective)
            await cache.ReplaceRolePermissionsAsync(roleId, permissionIds.ToList(), cancellationToken);
    }
}
