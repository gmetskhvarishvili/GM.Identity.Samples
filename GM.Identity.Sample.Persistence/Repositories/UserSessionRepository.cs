using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.Interfaces;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.Events.Users;
using GM.Identity.Sample.Persistence.Context;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Persistence.Repositories;

public class UserSessionRepository(ApplicationDbContext context)
    : GenericRepository<UserSession, ApplicationDbContext>(context), IUserSessionRepository
{
    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var sessions = await Query(true, null)
            .Where(x => x.UserId == userId && !x.IsRevoked)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
            return;

        foreach (var session in sessions)
            session.Revoke();

        UpdateRange(sessions);

        // Schedule reliable cache eviction: write the eviction event to the outbox in the same transaction as the
        // revocation, so it survives a crash and is relayed even if this process dies. Staged here; the caller's
        // SaveChangesAsync commits the revocations and the outbox message atomically.
        var tokenHashes = sessions.Select(x => x.TokenHash).ToList();
        _context.Set<OutboxMessage>().Add(
            OutboxMessage.From(userId, new SessionsRevokedIntegrationEvent(tokenHashes) { UserId = userId }));
    }
}
