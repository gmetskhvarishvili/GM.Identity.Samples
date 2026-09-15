using GM.Identity;
using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
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
}

public class EndSessionCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IRequestHandler<EndSessionCommand, EndSessionResponseDto>
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
            }
        }

        var redirectTo = await ResolveRedirectAsync(request, cancellationToken);
        return new EndSessionResponseDto(revokedSessions, redirectTo);
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
