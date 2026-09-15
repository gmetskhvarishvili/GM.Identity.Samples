using GM.Identity;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Infrastructure.Services.Logout;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Commands.EndSession;

/// <summary>
/// The OpenID Connect end-session (logout) endpoint, and the trigger for Single Logout. Ends the browser's SSO
/// session and cascades: every <c>UserSession</c> that was established through that SSO session (they carry its
/// id) is revoked and evicted from the session cache, so the user is signed out of every application at once —
/// their opaque access tokens stop validating immediately. Idempotent: an absent or unknown cookie revokes
/// nothing. When a <c>post_logout_redirect_uri</c> is supplied it is honoured only if it is a URI registered for
/// the named client (otherwise it is ignored, per the spec).
/// </summary>
public class EndSessionCommand : IRequest<EndSessionResponseDto>
{
    /// <summary>The opaque SSO cookie value presented by the browser.</summary>
    public string? SsoCookie { get; set; }

    /// <summary>Optional client whose registered URIs a supplied post-logout redirect is validated against.</summary>
    public Guid? ClientId { get; set; }

    /// <summary>Optional URI to return the user agent to after logout (must be registered for the client).</summary>
    public string? PostLogoutRedirectUri { get; set; }

    /// <summary>Optional opaque value echoed back on the post-logout redirect.</summary>
    public string? State { get; set; }

    /// <summary>This OP's issuer (scheme+host), used as the <c>iss</c> of back-channel logout tokens.</summary>
    public string? Issuer { get; set; }
}

public class EndSessionCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache,
    IBackchannelLogoutNotifier backchannelLogoutNotifier) : IRequestHandler<EndSessionCommand, EndSessionResponseDto>
{
    public async Task<EndSessionResponseDto> Handle(EndSessionCommand request, CancellationToken cancellationToken)
    {
        var revokedSessions = 0;

        if (!string.IsNullOrWhiteSpace(request.SsoCookie))
        {
            var hash = TokenGenerator.Hash(request.SsoCookie);
            var ssoSession = await unitOfWork.SsoSessionRepository
                .Query(true, null)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAt == null, cancellationToken);

            if (ssoSession != null)
            {
                ssoSession.Revoke();
                unitOfWork.SsoSessionRepository.Update(ssoSession);

                // Cascade to every app session spawned through this SSO session (Single Logout).
                var appSessions = await unitOfWork.UserSessionRepository
                    .Query(true, null)
                    .Where(x => x.SsoSessionId == ssoSession.Id && !x.IsRevoked)
                    .ToListAsync(cancellationToken);

                foreach (var session in appSessions)
                    session.Revoke();
                if (appSessions.Count > 0)
                    unitOfWork.UserSessionRepository.UpdateRange(appSessions);

                await unitOfWork.SaveChangesAsync(cancellationToken);

                foreach (var session in appSessions)
                    await sessionCache.RemoveAsync(session.TokenHash, cancellationToken);

                revokedSessions = appSessions.Count;

                // OIDC back-channel logout: tell every relying party that registered a logout endpoint to end
                // its own session too, so logout truly spans applications and not just this OP's sessions.
                await NotifyRelyingPartiesAsync(request, appSessions, ssoSession.Id, cancellationToken);
            }
        }

        var redirectTo = await ResolveRedirectAsync(request, cancellationToken);
        return new EndSessionResponseDto(revokedSessions, redirectTo);
    }

    // Builds one back-channel logout target per relying party that (a) had a session under this SSO session and
    // (b) registered a BackchannelLogoutUri, then hands them to the notifier (sub = user, sid = SSO session).
    private async Task NotifyRelyingPartiesAsync(
        EndSessionCommand request, IReadOnlyCollection<Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.UserSession> appSessions,
        Guid ssoSessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Issuer))
            return;

        var clientIds = appSessions
            .Where(s => s.ClientId is not null && s.UserId is not null)
            .Select(s => s.ClientId!.Value)
            .Distinct()
            .ToList();
        if (clientIds.Count == 0)
            return;

        var logoutUris = await unitOfWork.ClientRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .Where(c => clientIds.Contains(c.Id) && c.BackchannelLogoutUri != null)
            .Select(c => new { c.Id, c.BackchannelLogoutUri })
            .ToListAsync(cancellationToken);
        if (logoutUris.Count == 0)
            return;

        var uriByClient = logoutUris.ToDictionary(c => c.Id, c => c.BackchannelLogoutUri!);

        // One notification per client (dedupe across a client's multiple sessions), naming the user it held.
        var targets = appSessions
            .Where(s => s.ClientId is { } cid && uriByClient.ContainsKey(cid) && s.UserId is not null)
            .GroupBy(s => s.ClientId!.Value)
            .Select(g => new BackchannelLogoutTarget(
                g.Key, uriByClient[g.Key], g.First().UserId!.Value, ssoSessionId))
            .ToList();

        if (targets.Count > 0)
            await backchannelLogoutNotifier.NotifyAsync(request.Issuer!, targets, cancellationToken);
    }

    // A post-logout redirect is honoured only when it is a URI registered for the named client — never a
    // caller-supplied open redirect. State is echoed back when present.
    private async Task<string?> ResolveRedirectAsync(EndSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PostLogoutRedirectUri) || request.ClientId is not { } clientId)
            return null;

        var registered = await unitOfWork.ClientRedirectUriRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .AnyAsync(x => x.ClientId == clientId && x.Uri == request.PostLogoutRedirectUri
                           && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (!registered)
            return null;

        var redirectTo = request.PostLogoutRedirectUri!;
        if (!string.IsNullOrEmpty(request.State))
        {
            var separator = redirectTo.Contains('?') ? '&' : '?';
            redirectTo += $"{separator}state={Uri.EscapeDataString(request.State)}";
        }

        return redirectTo;
    }
}

public record EndSessionResponseDto(int RevokedSessions, string? RedirectTo);
