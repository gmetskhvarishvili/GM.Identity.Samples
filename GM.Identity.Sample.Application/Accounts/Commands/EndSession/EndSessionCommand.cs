using GM.Identity;
using GM.Identity.Authorization;
using GM.Identity.Oidc;
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

    /// <summary>An id_token previously issued to the RP, identifying the session to end when no cookie is present.</summary>
    public string? IdTokenHint { get; set; }
}

public class EndSessionCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache,
    IIdTokenReader idTokenReader,
    IBackchannelLogoutNotifier backchannelLogoutNotifier) : IRequestHandler<EndSessionCommand, EndSessionResponseDto>
{
    public async Task<EndSessionResponseDto> Handle(EndSessionCommand request, CancellationToken cancellationToken)
    {
        var revokedSessions = 0;
        IReadOnlyCollection<string> frontChannelLogoutUris = System.Array.Empty<string>();

        var ssoSession = await ResolveSsoSessionAsync(request, cancellationToken);
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

            // OIDC logout propagation: notify relying parties so logout truly spans applications, not just
            // this OP's sessions — via back-channel POSTs and/or front-channel iframe URLs.
            frontChannelLogoutUris = await PropagateLogoutAsync(request, appSessions, ssoSession.Id, cancellationToken);
        }

        var redirectTo = await ResolveRedirectAsync(request, cancellationToken);
        return new EndSessionResponseDto(revokedSessions, redirectTo, frontChannelLogoutUris);
    }

    // Identifies the SSO session to end — from the browser's SSO cookie, or (when there is none) from a signed
    // id_token_hint whose sid names it. Only a live (non-revoked) session is returned.
    private async Task<Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate.SsoSession?>
        ResolveSsoSessionAsync(EndSessionCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.SsoCookie))
        {
            var hash = TokenGenerator.Hash(request.SsoCookie);
            return await unitOfWork.SsoSessionRepository
                .Query(true, null)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAt == null, cancellationToken);
        }

        var sessionId = await idTokenReader.TryReadSessionIdAsync(request.IdTokenHint, cancellationToken);
        if (sessionId is not { } id)
            return null;

        return await unitOfWork.SsoSessionRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.RevokedAt == null, cancellationToken);
    }

    // For every relying party that had a session under this SSO session: POST a back-channel logout token to
    // those with a BackchannelLogoutUri, and build an iframe URL (with iss + sid) for those with a
    // FrontchannelLogoutUri. Returns the front-channel URLs for the caller to render.
    private async Task<IReadOnlyCollection<string>> PropagateLogoutAsync(
        EndSessionCommand request,
        IReadOnlyCollection<Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.UserSession> appSessions,
        Guid ssoSessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Issuer))
            return System.Array.Empty<string>();

        var clientIds = appSessions
            .Where(s => s.ClientId is not null && s.UserId is not null)
            .Select(s => s.ClientId!.Value)
            .Distinct()
            .ToList();
        if (clientIds.Count == 0)
            return System.Array.Empty<string>();

        var clients = await unitOfWork.ClientRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .Where(c => clientIds.Contains(c.Id)
                        && (c.BackchannelLogoutUri != null || c.FrontchannelLogoutUri != null))
            .Select(c => new { c.Id, c.BackchannelLogoutUri, c.FrontchannelLogoutUri })
            .ToListAsync(cancellationToken);
        if (clients.Count == 0)
            return System.Array.Empty<string>();

        // The user each client held (dedupe across a client's multiple sessions).
        var userByClient = appSessions
            .Where(s => s.ClientId is not null && s.UserId is not null)
            .GroupBy(s => s.ClientId!.Value)
            .ToDictionary(g => g.Key, g => g.First().UserId!.Value);

        // Back-channel: POST a signed logout token to each RP that registered one.
        var backchannelTargets = clients
            .Where(c => c.BackchannelLogoutUri is not null && userByClient.ContainsKey(c.Id))
            .Select(c => new BackchannelLogoutTarget(c.Id, c.BackchannelLogoutUri!, userByClient[c.Id], ssoSessionId))
            .ToList();
        if (backchannelTargets.Count > 0)
            await backchannelLogoutNotifier.NotifyAsync(request.Issuer!, backchannelTargets, cancellationToken);

        // Front-channel: an iframe URL per RP, carrying the issuer and session id per the spec.
        return clients
            .Where(c => c.FrontchannelLogoutUri is not null)
            .Select(c => BuildFrontChannelUrl(c.FrontchannelLogoutUri!, request.Issuer!, ssoSessionId))
            .ToList();
    }

    private static string BuildFrontChannelUrl(string uri, string issuer, Guid ssoSessionId)
    {
        var separator = uri.Contains('?') ? '&' : '?';
        return $"{uri}{separator}iss={Uri.EscapeDataString(issuer)}&sid={Uri.EscapeDataString(ssoSessionId.ToString())}";
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

public record EndSessionResponseDto(
    int RevokedSessions,
    string? RedirectTo,
    IReadOnlyCollection<string> FrontChannelLogoutUris = null!);
