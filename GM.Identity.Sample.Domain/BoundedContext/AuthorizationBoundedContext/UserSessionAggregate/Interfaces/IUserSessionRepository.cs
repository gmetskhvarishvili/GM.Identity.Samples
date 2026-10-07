using GM.EntityFramework.Domain.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.Interfaces;

public interface IUserSessionRepository : IGenericRepository<UserSession>
{
    /// <summary>
    /// Marks all of a user's active sessions revoked. Each revocation raises a <c>GMUserSessionRevokedDomainEvent</c>,
    /// which — after the caller commits — drives the background job that evicts the session from the cache. Staged
    /// only; the caller commits via the unit of work.
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}