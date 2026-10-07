using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.Interfaces;
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

        // Each Revoke() raises GMUserSessionRevokedDomainEvent; after SaveChangesAsync the dispatcher hands each
        // to UserSessionRevokedCacheEvictionHandler, which fires the background job evicting that session from
        // the session cache. So eviction is driven by the domain events here — no outbox message needed.
        foreach (var session in sessions)
            session.Revoke();

        UpdateRange(sessions);
    }
}
