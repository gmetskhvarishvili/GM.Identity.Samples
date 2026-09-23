using GM.EntityFramework.Domain.Repositories;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.Interfaces;

public interface IUserSessionRepository : IGenericRepository<UserSession>
{
    /// <summary>
    /// Marks all of a user's active sessions revoked and queues a <c>SessionsRevoked</c> outbox message (in the
    /// same unit of work) so their session-cache entries are reliably evicted downstream. Staged only — the
    /// caller commits via the unit of work.
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}