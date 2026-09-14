using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Revokes all of a client's active sessions (DB + Redis) as one step, so a security-relevant change —
/// rotating the secret or deactivating the client — takes effect immediately instead of leaving the client's
/// existing access tokens valid until they expire.
/// </summary>
public static class ClientSessionRevocation
{
    public static async Task RevokeAllClientSessionsAsync(
        this IUnitOfWork unitOfWork, ISessionCache sessionCache, Guid clientId, CancellationToken cancellationToken)
    {
        var sessions = await unitOfWork.ClientSessionRepository
            .Query(true, null)
            .Where(x => x.ClientId == clientId && !x.IsRevoked)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0) return;

        foreach (var session in sessions)
            session.Revoke();

        unitOfWork.ClientSessionRepository.UpdateRange(sessions);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var session in sessions)
            await sessionCache.RemoveAsync(session.TokenHash, cancellationToken);
    }
}
