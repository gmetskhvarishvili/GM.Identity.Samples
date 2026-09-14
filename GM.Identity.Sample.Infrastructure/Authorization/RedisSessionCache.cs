using System.Text.Json;
using GM.Identity.Sample.Application.Common.Authorization;
using StackExchange.Redis;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// <see cref="ISessionCache"/> over StackExchange.Redis. Keys are <c>gm-identity:auth:session:{tokenHash}</c>
/// (the token hash, matching <c>UserSession.TokenHash</c> â€” raw tokens are never stored). The value is the
/// JSON-serialized <see cref="SessionInfo"/>, with a Redis TTL set to the token's remaining lifetime so
/// stale sessions self-evict. The gateway reads the same key to validate an incoming bearer token.
/// </summary>
public sealed class RedisSessionCache(IConnectionMultiplexer connection) : ISessionCache
{
    internal const string Prefix = "gm-identity:auth:session:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SetAsync(string tokenHash, SessionInfo info, CancellationToken cancellationToken = default)
    {
        var ttl = info.ExpiresAt - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero) return; // already expired â€” nothing worth caching

        await connection.GetDatabase()
            .StringSetAsync(Prefix + tokenHash, JsonSerializer.Serialize(info, JsonOptions), ttl);
    }

    public Task RemoveAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        connection.GetDatabase().KeyDeleteAsync(Prefix + tokenHash);

    public async Task<IReadOnlyCollection<string>> GetCachedTokenHashesAsync(CancellationToken cancellationToken = default)
    {
        var hashes = new List<string>();
        foreach (var endpoint in connection.GetEndPoints())
        {
            var server = connection.GetServer(endpoint);
            if (!server.IsConnected || server.IsReplica) continue;

            await foreach (var key in server.KeysAsync(pattern: Prefix + "*"))
                hashes.Add(((string)key!)[Prefix.Length..]);
        }
        return hashes;
    }
}
