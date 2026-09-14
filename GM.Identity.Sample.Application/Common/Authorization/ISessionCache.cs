using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Common.Authorization;

/// <summary>
/// A Redis-backed lookup of active sessions, keyed by the token hash (the same value stored as
/// <c>UserSession.TokenHash</c>, so raw tokens are never at rest). Written when a session is created and
/// removed when it is revoked; entries also expire on their own at the token's expiry. The gateway reads
/// this to validate an opaque bearer token and resolve the acting user/session without a DB round-trip.
/// </summary>
public interface ISessionCache
{
    /// <summary>Stores the session under <paramref name="tokenHash"/> with a TTL derived from <see cref="SessionInfo.ExpiresAt"/>.</summary>
    Task SetAsync(string tokenHash, SessionInfo info, CancellationToken cancellationToken = default);

    /// <summary>Removes the session (e.g. on revoke/logout).</summary>
    Task RemoveAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>All token hashes that currently have a cached session (for reconciliation orphan pruning).</summary>
    Task<IReadOnlyCollection<string>> GetCachedTokenHashesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The session data resolved from a token. Serialized as JSON (web/camelCase) into Redis.
/// <see cref="UserId"/> is null for a client-credentials (client) session.
/// </summary>
public sealed record SessionInfo(Guid? UserId, Guid SessionId, Guid? ClientId, DateTime ExpiresAt);
