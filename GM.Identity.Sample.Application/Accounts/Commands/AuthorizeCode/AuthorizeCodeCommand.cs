using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserClientConsentAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Commands.AuthorizeCode;

/// <summary>
/// The authorization endpoint of the PKCE authorization-code flow, and the entry point for cross-application
/// single sign-on. It works in two modes:
/// <list type="bullet">
/// <item><b>Silent (SSO):</b> when the caller presents a valid SSO session cookie (and does not force a fresh
/// login), the resource owner is taken from that session — no credentials are needed. This is what lets a
/// second, third, … client obtain a code without the user signing in again.</item>
/// <item><b>Interactive:</b> otherwise the user's credentials are verified and a <b>new SSO session</b> is
/// established (its opaque cookie is returned for the caller to set), so subsequent clients can go silent.</item>
/// </list>
/// The minted authorization code carries the SSO session id so the token exchange can stamp it on the issued
/// <c>UserSession</c>, enabling Single Logout to cascade. <c>prompt=none</c> requires an existing SSO session
/// (never prompts); <c>prompt=login</c> forces re-authentication even when one exists.
/// </summary>
public class AuthorizeCodeCommand : IRequest<AuthorizeCodeResponseDto>
{
    public Guid ClientId { get; set; }
    public string RedirectUri { get; set; } = null!;
    public string? Scope { get; set; }
    public string? State { get; set; }

    /// <summary>PKCE code challenge — BASE64URL(SHA256(code_verifier)).</summary>
    public string CodeChallenge { get; set; } = null!;

    /// <summary>PKCE method; only <c>S256</c> is supported.</summary>
    public string CodeChallengeMethod { get; set; } = "S256";

    /// <summary>Resource-owner credentials — required only when there is no usable SSO session.</summary>
    public string? UserName { get; set; }
    public string? Password { get; set; }

    /// <summary>The opaque SSO cookie value presented by the browser, if any (enables silent authorization).</summary>
    public string? SsoCookie { get; set; }

    /// <summary>OIDC <c>prompt</c>: <c>none</c> = never prompt (fail if no SSO session); <c>login</c> = force re-auth.</summary>
    public string? Prompt { get; set; }

    /// <summary>OIDC <c>nonce</c> — echoed into the id_token at token exchange to bind it to this request.</summary>
    public string? Nonce { get; set; }

    /// <summary>The user's approval of the requested scopes, for consent-requiring clients (also implied by prompt=consent).</summary>
    public bool Consent { get; set; }
}

public class AuthorizeCodeCommandValidator : AbstractValidator<AuthorizeCodeCommand>
{
    public AuthorizeCodeCommandValidator()
    {
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.RedirectUri).NotNull().NotEmpty();
        RuleFor(x => x.CodeChallenge).NotNull().NotEmpty();
        RuleFor(x => x.CodeChallengeMethod).Equal("S256").WithMessage("Only the S256 PKCE method is supported.");
        // UserName/Password are validated in the handler: they are required only when the request cannot be
        // satisfied silently from an SSO session cookie.
    }
}

public class AuthorizeCodeCommandHandler(
    IUnitOfWork unitOfWork,
    IOptions<AuthOptions> options) : IRequestHandler<AuthorizeCodeCommand, AuthorizeCodeResponseDto>
{
    // Authorization codes are short-lived by design (RFC 6749 §4.1.2 recommends <= 10 minutes; we use 1).
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(1);

    public async Task<AuthorizeCodeResponseDto> Handle(AuthorizeCodeCommand request, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.ClientId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (client == null)
            throw new NotFoundException(StringResource.Client, StringResource.Id, request.ClientId);

        // The redirect URI must be one registered for the client (exact match) — never a caller-supplied one.
        var redirectRegistered = await unitOfWork.ClientRedirectUriRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .AnyAsync(x => x.ClientId == request.ClientId && x.Uri == request.RedirectUri
                           && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (!redirectRegistered)
            throw new ValidationException("The redirect URI is not registered for this client.");

        var now = DateTime.UtcNow;
        var settings = options.Value;
        var forceLogin = string.Equals(request.Prompt, "login", StringComparison.OrdinalIgnoreCase);
        var promptNone = string.Equals(request.Prompt, "none", StringComparison.OrdinalIgnoreCase);

        // 1) Try to satisfy the request silently from an existing SSO session (unless a fresh login is forced).
        SsoSession? ssoSession = null;
        if (!forceLogin && !string.IsNullOrWhiteSpace(request.SsoCookie))
            ssoSession = await ResolveSsoSessionAsync(request.SsoCookie!, now, cancellationToken);

        Guid userId;
        string? newSsoCookie = null;
        DateTime? newSsoCookieExpiresAt = null;

        if (ssoSession != null)
        {
            // Silent authorization: the SSO session names the resource owner. No credentials required.
            userId = ssoSession.UserId;
        }
        else
        {
            // 2) No usable SSO session. prompt=none forbids prompting, so fail per OIDC.
            if (promptNone)
                throw new ValidationException("login_required: no active SSO session for silent authorization.");

            // Interactive login: verify credentials and establish a new SSO session.
            var user = await AuthenticateAsync(request, now, cancellationToken);
            userId = user.Id;

            newSsoCookie = TokenGenerator.Generate();
            newSsoCookieExpiresAt = now.AddMinutes(settings.SsoSessionMinutes);
            ssoSession = SsoSession.Create(
                user.Id, TokenGenerator.Hash(newSsoCookie), authTime: now, expiresAt: newSsoCookieExpiresAt.Value);
            await unitOfWork.SsoSessionRepository.AddAsync(ssoSession, cancellationToken);
        }

        // 2b) Consent gate (opt-in per client): a consent-requiring client needs the user's approval of the
        // requested scopes unless a prior consent already covers them.
        if (client.RequireConsent)
            await EnsureConsentAsync(request, userId, cancellationToken);

        // 3) Mint the single-use authorization code, tagged with the SSO session for logout cascade.
        var code = TokenGenerator.Generate();
        var authCode = AuthorizationCode.Create(
            request.ClientId, userId, TokenGenerator.Hash(code), request.RedirectUri,
            request.Scope, request.CodeChallenge, request.CodeChallengeMethod,
            now.Add(CodeLifetime), ssoSession.Id, request.Nonce);

        await unitOfWork.AuthorizationCodeRepository.AddAsync(authCode, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // The caller redirects the user agent here (code + state on the registered redirect URI).
        var separator = request.RedirectUri.Contains('?') ? '&' : '?';
        var redirectTo = $"{request.RedirectUri}{separator}code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrEmpty(request.State))
            redirectTo += $"&state={Uri.EscapeDataString(request.State)}";

        return new AuthorizeCodeResponseDto(code, request.State, redirectTo, newSsoCookie, newSsoCookieExpiresAt);
    }

    // Loads an active (non-revoked, unexpired) SSO session by its cookie hash, but only if the user it names is
    // still active and not blocked — a since-blocked user must not keep riding an SSO session silently.
    private async Task<SsoSession?> ResolveSsoSessionAsync(
        string ssoCookie, DateTime now, CancellationToken cancellationToken)
    {
        var hash = TokenGenerator.Hash(ssoCookie);
        var session = await unitOfWork.SsoSessionRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAt == null && x.ExpiresAt > now
                                      && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (session == null)
            return null;

        var userActive = await unitOfWork.UserRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id == session.UserId && !x.IsBlocked
                           && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);

        return userActive ? session : null;
    }

    // Verifies the resource owner's credentials (cross-tenant, like the token endpoint). Requires that the
    // request actually carried credentials, since the silent path did not apply.
    private async Task<User> AuthenticateAsync(
        AuthorizeCodeCommand request, DateTime now, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        var user = await unitOfWork.UserRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserName == request.UserName && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);

        if (user == null || user.IsBlocked
            || (user.LockoutEnd is { } lockoutEnd && lockoutEnd > now)
            || !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new ValidationException(ExceptionsResource.InvalidCredentials);
        }

        return user;
    }

    // Enforces per-client consent: proceeds silently if a prior consent already covers the requested scopes;
    // otherwise requires the user's approval (request.Consent, or prompt=consent) and records/widens the grant,
    // or refuses with consent_required.
    private async Task EnsureConsentAsync(AuthorizeCodeCommand request, Guid userId, CancellationToken cancellationToken)
    {
        var requested = ParseScopes(request.Scope);
        if (requested.Count == 0)
            return; // nothing to consent to

        var consent = await unitOfWork.UserClientConsentRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ClientId == request.ClientId
                                      && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);

        var consented = consent is null ? new HashSet<string>() : ParseScopes(consent.Scopes);
        if (requested.IsSubsetOf(consented))
            return; // already covered

        var approves = request.Consent || string.Equals(request.Prompt, "consent", StringComparison.OrdinalIgnoreCase);
        if (!approves)
            throw new ValidationException("consent_required: the user must approve the requested scopes.");

        // Record the approval, widening any existing grant to the union.
        consented.UnionWith(requested);
        var scopes = string.Join(' ', consented.OrderBy(s => s, StringComparer.Ordinal));
        if (consent is null)
            await unitOfWork.UserClientConsentRepository.AddAsync(
                UserClientConsent.Create(userId, request.ClientId, scopes), cancellationToken);
        else
        {
            consent.Grant(scopes);
            unitOfWork.UserClientConsentRepository.Update(consent);
        }
    }

    private static HashSet<string> ParseScopes(string? scope) =>
        string.IsNullOrWhiteSpace(scope)
            ? new HashSet<string>()
            : new HashSet<string>(scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), StringComparer.Ordinal);
}

public record AuthorizeCodeResponseDto(
    string Code,
    string? State,
    string RedirectTo,
    string? SsoCookie = null,
    DateTime? SsoCookieExpiresAt = null);
