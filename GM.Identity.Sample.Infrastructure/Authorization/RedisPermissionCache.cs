using GM.Identity.Sample.Application.Common.Authorization;
using StackExchange.Redis;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// <see cref="IPermissionCache"/> over StackExchange.Redis native SETs. Keys are namespaced under
/// <c>gm-identity:auth:</c>: <c>userRoles:{userId}</c> holds a user's role ids, <c>rolePermissions:{roleId}</c>
/// holds a role's permission ids. Membership checks are O(1) (<c>SISMEMBER</c>); a permission check reads
/// the user's roles once then pipelines a <c>SISMEMBER</c> per role.
/// </summary>
public sealed class RedisPermissionCache(IConnectionMultiplexer connection) : IPermissionCache
{
    private const string Prefix = "gm-identity:auth:";
    private const string UserRolesPrefix = Prefix + "userRoles:";
    private const string RolePermissionsPrefix = Prefix + "rolePermissions:";
    private const string PermissionNamePrefix = Prefix + "permissionName:";

    private static RedisKey UserRolesKey(Guid userId) => UserRolesPrefix + userId.ToString("D");
    private static RedisKey RolePermissionsKey(Guid roleId) => RolePermissionsPrefix + roleId.ToString("D");

    private IDatabase Db => connection.GetDatabase();

    // ---- user â†’ roles ----
    public Task AddUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default) =>
        Db.SetAddAsync(UserRolesKey(userId), roleId.ToString("D"));

    public Task RemoveUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default) =>
        Db.SetRemoveAsync(UserRolesKey(userId), roleId.ToString("D"));

    public Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default) =>
        ReplaceSetAsync(UserRolesKey(userId), roleIds);

    public Task RemoveUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Db.KeyDeleteAsync(UserRolesKey(userId));

    public async Task<IReadOnlyCollection<Guid>> GetUserRoleIdsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        ParseGuids(await Db.SetMembersAsync(UserRolesKey(userId)));

    // ---- role â†’ permissions ----
    public Task AddRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default) =>
        Db.SetAddAsync(RolePermissionsKey(roleId), permissionId.ToString("D"));

    public Task RemoveRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default) =>
        Db.SetRemoveAsync(RolePermissionsKey(roleId), permissionId.ToString("D"));

    public Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken = default) =>
        ReplaceSetAsync(RolePermissionsKey(roleId), permissionIds);

    public Task RemoveRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        Db.KeyDeleteAsync(RolePermissionsKey(roleId));

    // ---- authorization check ----
    public async Task<bool> HasPermissionAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var db = Db;
        var roles = await db.SetMembersAsync(UserRolesKey(userId));
        if (roles.Length == 0) return false;

        var target = (RedisValue)permissionId.ToString("D");
        // Pipeline one SISMEMBER per role; StackExchange.Redis batches these on the wire.
        var checks = new List<Task<bool>>(roles.Length);
        foreach (var role in roles)
            if (Guid.TryParse((string?)role, out var roleId))
                checks.Add(db.SetContainsAsync(RolePermissionsKey(roleId), target));

        var results = await Task.WhenAll(checks);
        return Array.Exists(results, granted => granted);
    }

    // ---- permission name → id resolution ----
    public Task SetPermissionIdAsync(string permissionName, Guid permissionId, CancellationToken cancellationToken = default) =>
        Db.StringSetAsync(PermissionNamePrefix + permissionName, permissionId.ToString("D"));

    public async Task<Guid?> GetPermissionIdByNameAsync(string permissionName, CancellationToken cancellationToken = default)
    {
        var value = await Db.StringGetAsync(PermissionNamePrefix + permissionName);
        return Guid.TryParse((string?)value, out var id) ? id : null;
    }

    // ---- reconciliation support ----
    public Task<IReadOnlyCollection<Guid>> GetCachedUserIdsAsync(CancellationToken cancellationToken = default) =>
        ScanKeySuffixesAsync(UserRolesPrefix);

    public Task<IReadOnlyCollection<Guid>> GetCachedRoleIdsAsync(CancellationToken cancellationToken = default) =>
        ScanKeySuffixesAsync(RolePermissionsPrefix);

    // ---- helpers ----
    private async Task ReplaceSetAsync(RedisKey key, IReadOnlyCollection<Guid> ids)
    {
        var db = Db;
        var transaction = db.CreateTransaction();
        _ = transaction.KeyDeleteAsync(key);
        if (ids.Count > 0)
            _ = transaction.SetAddAsync(key, ids.Select(id => (RedisValue)id.ToString("D")).ToArray());
        await transaction.ExecuteAsync();
    }

    private async Task<IReadOnlyCollection<Guid>> ScanKeySuffixesAsync(string keyPrefix)
    {
        var ids = new List<Guid>();
        foreach (var endpoint in connection.GetEndPoints())
        {
            var server = connection.GetServer(endpoint);
            if (!server.IsConnected || server.IsReplica) continue;

            await foreach (var key in server.KeysAsync(pattern: keyPrefix + "*"))
            {
                var suffix = ((string)key!)[keyPrefix.Length..];
                if (Guid.TryParse(suffix, out var id)) ids.Add(id);
            }
        }
        return ids;
    }

    private static List<Guid> ParseGuids(RedisValue[] values)
    {
        var ids = new List<Guid>(values.Length);
        foreach (var value in values)
            if (Guid.TryParse((string?)value, out var id)) ids.Add(id);
        return ids;
    }
}
