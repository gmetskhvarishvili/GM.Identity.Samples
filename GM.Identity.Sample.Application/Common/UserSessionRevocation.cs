using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Revokes all of a user's active sessions (DB + Redis) as one step, so a security-relevant change —
/// blocking, deactivating, or a password reset/change — takes effect immediately instead of leaving the
/// user's existing access/refresh tokens valid until they expire.
/// </summary>
public static class UserSessionRevocation
{
    public static async Task RevokeAllUserSessionsAsync(
        this IUnitOfWork unitOfWork, ISessionCache sessionCache, Guid userId, CancellationToken cancellationToken)
    {
        var sessions = await unitOfWork.UserSessionRepository
            .Query(true, null)
            .Where(x => x.UserId == userId && !x.IsRevoked)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0) return;

        foreach (var session in sessions)
            session.Revoke();

        unitOfWork.UserSessionRepository.UpdateRange(sessions);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var session in sessions)
            await sessionCache.RemoveAsync(session.TokenHash, cancellationToken);
    }
}
