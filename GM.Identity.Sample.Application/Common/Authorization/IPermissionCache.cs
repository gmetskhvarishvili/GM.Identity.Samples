using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Common.Authorization;

/// <summary>
/// A Redis-backed projection of the RBAC graph, kept for fast authorization checks:
/// <c>userId â†’ {roleId}</c> and <c>roleId â†’ {permissionId}</c> (both native Redis SETs). The database
/// is authoritative; this cache is updated write-through by the command handlers and reconciled
/// periodically by <c>PermissionCacheReconciliationJob</c>.
/// </summary>
public interface IPermissionCache
{
    // ---- user â†’ roles ----
    Task AddUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task RemoveUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Overwrites a user's role set atomically (used by reconciliation).</summary>
    Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>Drops the user's role set entirely (e.g. the user was deleted).</summary>
    Task RemoveUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> GetUserRoleIdsAsync(Guid userId, CancellationToken cancellationToken = default);

    // ---- role â†’ permissions ----
    Task AddRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task RemoveRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>Overwrites a role's permission set atomically (used by reconciliation).</summary>
    Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>Drops the role's permission set entirely (e.g. the role was deleted).</summary>
    Task RemoveRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    // ---- authorization check ----
    /// <summary>True if any of the user's roles grants <paramref name="permissionId"/>.</summary>
    Task<bool> HasPermissionAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default);

    // ---- permission name → id resolution (permissions are addressed by name, e.g. an action name) ----
    /// <summary>Maps a permission name to its id, so callers can check by name (seeded at startup).</summary>
    Task SetPermissionIdAsync(string permissionName, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>Resolves a permission name to its id, or <c>null</c> if the name is unknown.</summary>
    Task<Guid?> GetPermissionIdByNameAsync(string permissionName, CancellationToken cancellationToken = default);

    // ---- reconciliation support ----
    /// <summary>All user ids that currently have a cached role set (for orphan detection).</summary>
    Task<IReadOnlyCollection<Guid>> GetCachedUserIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>All role ids that currently have a cached permission set (for orphan detection).</summary>
    Task<IReadOnlyCollection<Guid>> GetCachedRoleIdsAsync(CancellationToken cancellationToken = default);
}
