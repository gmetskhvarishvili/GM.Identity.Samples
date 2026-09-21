using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Recomputes a user's effective role set for the authorization cache from their directly-assigned roles.
/// Shared by the write-through paths so the cache matches what the reconcile job would build.
/// </summary>
public static class UserRoleProjection
{
    public static async Task RebuildUserAsync(
        IUnitOfWork unitOfWork, IPermissionCache cache, Guid userId, CancellationToken cancellationToken)
    {
        var roleIds = await unitOfWork.UserRoleRepository
            .Query(false, null).IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        await cache.ReplaceUserRolesAsync(userId, roleIds, cancellationToken);
    }
}
