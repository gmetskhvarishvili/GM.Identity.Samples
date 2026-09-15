using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Infrastructure.Services.Logout;

/// <summary>One relying party to notify of a logout: its client id, its back-channel logout endpoint, and the
/// subject (user) and SSO session that ended.</summary>
public sealed record BackchannelLogoutTarget(Guid ClientId, string LogoutUri, Guid UserId, Guid SessionId);

/// <summary>
/// Delivers OpenID Connect back-channel logout notifications. On Single Logout the OP POSTs a signed logout
/// token to each relying party's registered endpoint so the RP can terminate its own session server-side —
/// completing logout across applications without relying on the user agent.
/// </summary>
public interface IBackchannelLogoutNotifier
{
    /// <summary>
    /// Sends a logout token to every target. <paramref name="issuer"/> is this OP's issuer (the <c>iss</c>
    /// claim). Best-effort per target — a failing RP endpoint does not abort the others.
    /// </summary>
    Task NotifyAsync(string issuer, IReadOnlyCollection<BackchannelLogoutTarget> targets, CancellationToken cancellationToken);
}
