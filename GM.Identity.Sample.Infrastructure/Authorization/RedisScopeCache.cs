using GM.Identity.Sample.Application.Common.Authorization;
using StackExchange.Redis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// <see cref="IScopeCache"/> over StackExchange.Redis native SETs. Keys are namespaced under
/// <c>gm-identity:scope:</c>: <c>clientScopes:{clientId}</c> holds a client's granted scope ids,
/// <c>scopeOperations:{scopeId}</c> holds a scope's operation ids, and <c>operationName:{name}</c> maps an
/// operation name to its id. An operation check reads the client's scopes once then pipelines a
/// <c>SISMEMBER</c> per scope — the mirror image of <see cref="RedisPermissionCache"/>.
/// </summary>
public sealed class RedisScopeCache(IConnectionMultiplexer connection) : IScopeCache
{
    private const string Prefix = "gm-identity:scope:";
    private const string ClientScopesPrefix = Prefix + "clientScopes:";
    private const string ScopeOperationsPrefix = Prefix + "scopeOperations:";
    private const string OperationNamePrefix = Prefix + "operationName:";

    private static RedisKey ClientScopesKey(Guid clientId) => ClientScopesPrefix + clientId.ToString("D");
    private static RedisKey ScopeOperationsKey(Guid scopeId) => ScopeOperationsPrefix + scopeId.ToString("D");

    private IDatabase Db => connection.GetDatabase();

    // ---- client → scopes ----
    public Task AddClientScopeAsync(Guid clientId, Guid scopeId, CancellationToken cancellationToken = default) =>
        Db.SetAddAsync(ClientScopesKey(clientId), scopeId.ToString("D"));

    public Task RemoveClientScopeAsync(Guid clientId, Guid scopeId, CancellationToken cancellationToken = default) =>
        Db.SetRemoveAsync(ClientScopesKey(clientId), scopeId.ToString("D"));

    public Task ReplaceClientScopesAsync(Guid clientId, IReadOnlyCollection<Guid> scopeIds, CancellationToken cancellationToken = default) =>
        ReplaceSetAsync(ClientScopesKey(clientId), scopeIds);

    public Task RemoveClientAsync(Guid clientId, CancellationToken cancellationToken = default) =>
        Db.KeyDeleteAsync(ClientScopesKey(clientId));

    // ---- scope → operations ----
    public Task AddScopeOperationAsync(Guid scopeId, Guid operationId, CancellationToken cancellationToken = default) =>
        Db.SetAddAsync(ScopeOperationsKey(scopeId), operationId.ToString("D"));

    public Task RemoveScopeOperationAsync(Guid scopeId, Guid operationId, CancellationToken cancellationToken = default) =>
        Db.SetRemoveAsync(ScopeOperationsKey(scopeId), operationId.ToString("D"));

    public Task ReplaceScopeOperationsAsync(Guid scopeId, IReadOnlyCollection<Guid> operationIds, CancellationToken cancellationToken = default) =>
        ReplaceSetAsync(ScopeOperationsKey(scopeId), operationIds);

    public Task RemoveScopeAsync(Guid scopeId, CancellationToken cancellationToken = default) =>
        Db.KeyDeleteAsync(ScopeOperationsKey(scopeId));

    // ---- operation name → id resolution ----
    public Task SetOperationIdAsync(string operationName, Guid operationId, CancellationToken cancellationToken = default) =>
        Db.StringSetAsync(OperationNamePrefix + operationName, operationId.ToString("D"));

    public async Task<Guid?> GetOperationIdByNameAsync(string operationName, CancellationToken cancellationToken = default)
    {
        var value = await Db.StringGetAsync(OperationNamePrefix + operationName);
        return Guid.TryParse((string?)value, out var id) ? id : null;
    }

    // ---- authorization check ----
    public async Task<bool> HasOperationAsync(Guid clientId, Guid operationId, CancellationToken cancellationToken = default)
    {
        var db = Db;
        var scopes = await db.SetMembersAsync(ClientScopesKey(clientId));
        if (scopes.Length == 0) return false;

        var target = (RedisValue)operationId.ToString("D");
        // Pipeline one SISMEMBER per scope; StackExchange.Redis batches these on the wire.
        var checks = new List<Task<bool>>(scopes.Length);
        foreach (var scope in scopes)
            if (Guid.TryParse((string?)scope, out var scopeId))
                checks.Add(db.SetContainsAsync(ScopeOperationsKey(scopeId), target));

        var results = await Task.WhenAll(checks);
        return Array.Exists(results, granted => granted);
    }

    // ---- reconciliation support ----
    public Task<IReadOnlyCollection<Guid>> GetCachedClientIdsAsync(CancellationToken cancellationToken = default) =>
        ScanKeySuffixesAsync(ClientScopesPrefix);

    public Task<IReadOnlyCollection<Guid>> GetCachedScopeIdsAsync(CancellationToken cancellationToken = default) =>
        ScanKeySuffixesAsync(ScopeOperationsPrefix);

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
}
