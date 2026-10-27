using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common;
using GM.Identity;
using GM.Identity.Authorization;
using GM.Identity.Oidc;
using GM.Identity.Sample.Application.Infrastructure.Services.OTP;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Application.Events.Users;
using GM.Identity.Sample.Domain.Enums;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using GM.OTP.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using ValidationException = GM.Exceptions.ValidationException;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Commands.Authorize;

public class AuthorizeCommand : IRequest<AuthorizeResponseDto>
{
    public string? UserName { get; set; }
    public string? Password { get; set; }

    public Guid ClientId { get; set; }
    public string ClientSecret { get; set; } = null!;

    public string GrantType { get; set; } = null!;

    /// <summary>Supplied with <c>grant_type=refresh_token</c> to rotate an existing session.</summary>
    public string? RefreshToken { get; set; }

    /// <summary>The one-time code, supplied with <c>grant_type=two_factor</c> to complete a 2FA login.</summary>
    public string? Code { get; set; }

    /// <summary>The redirect URI the authorization code was issued to (<c>grant_type=authorization_code</c>).</summary>
    public string? RedirectUri { get; set; }

    /// <summary>The PKCE code verifier proving possession of the authorization request (authorization_code grant).</summary>
    public string? CodeVerifier { get; set; }

    /// <summary>This OP's issuer (scheme+host), set by the controller; the <c>iss</c> of issued id_tokens.</summary>
    public string? Issuer { get; set; }
}

public class AuthorizeCommandValidator : AbstractValidator<AuthorizeCommand>
{
    public AuthorizeCommandValidator()
    {
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ClientSecret).NotNull().NotEmpty();
        RuleFor(x => x.GrantType).NotNull().NotEmpty();
    }
}

public class AuthorizeCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache,
    IOTPService otpService,
    ITotpCodeService totpService,
    IIdTokenGenerator idTokenGenerator,
    IAuthOptions options,
    IActorEstablisher actorEstablisher) : IRequestHandler<AuthorizeCommand, AuthorizeResponseDto>
{
    public async Task<AuthorizeResponseDto> Handle(AuthorizeCommand request, CancellationToken cancellationToken)
    {
        // Every grant authenticates the client first.
        var client = await unitOfWork.AuthenticateClientAsync(request.ClientId, request.ClientSecret, cancellationToken);

        var settings = options;
        var now = DateTime.UtcNow;

        return request.GrantType switch
        {
            "ClientCredentials" => await IssueClientTokenAsync(client.Id, client.TenantId, now, settings, cancellationToken),
            "refresh_token" => await RefreshAsync(request, client.Id, now, settings, cancellationToken),
            "two_factor" => await TwoFactorGrantAsync(request, client.Id, now, settings, cancellationToken),
            "authorization_code" => await AuthorizationCodeGrantAsync(request, client.Id, now, settings, cancellationToken),
            _ => await PasswordGrantAsync(request, client.Id, now, settings, cancellationToken),
        };
    }

    private async Task<AuthorizeResponseDto> PasswordGrantAsync(
        AuthorizeCommand request, Guid clientId, DateTime now, IAuthOptions settings, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.AuthenticateUserAsync(request.UserName, request.Password, now, settings, cancellationToken);

        // Two-factor step-up: if the user has any second factor — an enrolled method or a confirmed
        // authenticator-app (TOTP) device — the password alone does not mint a session. Return a challenge naming
        // the enrolled methods; the caller picks where the code goes (POST /connect/two-factor/send) and completes
        // login with grant_type=two_factor + the code. With exactly one delivery method and no authenticator app
        // there is nothing to choose, so the code is sent right away.
        var twoFactorTypeIds = await unitOfWork.GetEnrolledTwoFactorTypeIdsAsync(user.Id, cancellationToken);
        var hasTotp = await GetConfirmedTotpSecretAsync(user.Id, cancellationToken) != null;
        if (twoFactorTypeIds.Count > 0 || hasTotp)
        {
            var deliveryMethods = TwoFactorDelivery.DeliveryMethods(twoFactorTypeIds);
            TwoFactorMethod? sentVia = null;
            if (deliveryMethods.Count == 1 && !hasTotp)
            {
                sentVia = deliveryMethods.Single();
                await unitOfWork.QueueTwoFactorCodeAsync(user, sentVia.Value, cancellationToken);
            }

            return AuthorizeResponseDto.TwoFactorChallenge(twoFactorTypeIds, hasTotp, (int?)sentVia);
        }

        return await IssueUserSessionAsync(
            user.Id, clientId, provider: null, now, settings,
            refreshExpiry: now.AddDays(settings.RefreshTokenDays), cancellationToken, issuer: request.Issuer);
    }

    private async Task<AuthorizeResponseDto> TwoFactorGrantAsync(
        AuthorizeCommand request, Guid clientId, DateTime now, IAuthOptions settings, CancellationToken cancellationToken)
    {
        // Completing a 2FA login: re-verify the password (the challenge is not itself a bearer of trust),
        // require that the user really is enrolled, then validate the one-time code before issuing a session.
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        var user = await unitOfWork.AuthenticateUserAsync(request.UserName, request.Password, now, settings, cancellationToken);

        var twoFactorTypeIds = await unitOfWork.GetEnrolledTwoFactorTypeIdsAsync(user.Id, cancellationToken);
        var totpSecret = await GetConfirmedTotpSecretAsync(user.Id, cancellationToken);
        if (twoFactorTypeIds.Count == 0 && totpSecret == null)
            throw new ValidationException("This account has no second factor configured.");

        // The submitted value can be an authenticator-app (TOTP) code or the one-time code sent to the user's
        // contact. Try each in turn; only if none matches is the second factor rejected.
        var subject = TwoFactorDelivery.TwoFactorSubject(user);
        var verified =
            (totpSecret != null && totpService.VerifyCode(totpSecret, request.Code, DateTime.UtcNow))
            || (twoFactorTypeIds.Count > 0 && !string.IsNullOrWhiteSpace(subject)
                && await TryVerifyOtpAsync(subject, request.Code, cancellationToken));

        if (!verified)
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        return await IssueUserSessionAsync(
            user.Id, clientId, provider: null, now, settings,
            refreshExpiry: now.AddDays(settings.RefreshTokenDays), cancellationToken, issuer: request.Issuer);
    }

    // Validates the submitted value as the one-time code issued to the user's contact. Returns false (rather
    // than throwing) so the caller can fall back to a recovery code.
    private async Task<bool> TryVerifyOtpAsync(string subject, string code, CancellationToken cancellationToken)
    {
        try
        {
            await otpService.VerifyOTP(
                new VerifyOTPDto { Subject = subject, Purpose = OtpPurpose.TwoFactor, Code = code },
                cancellationToken);
            return true;
        }
        catch (ValidationException)
        {
            return false; // Not a valid OTP.
        }
    }

    // The user's confirmed authenticator-app (TOTP) secret, or null if they have none. A pending (unconfirmed)
    // device never gates login.
    private async Task<string?> GetConfirmedTotpSecretAsync(Guid userId, CancellationToken cancellationToken) =>
        await unitOfWork.UserTotpDeviceRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.IsConfirmed && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.SecretBase32)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<AuthorizeResponseDto> RefreshAsync(
        AuthorizeCommand request, Guid clientId, DateTime now, IAuthOptions settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        var refreshHash = TokenGenerator.Hash(request.RefreshToken);
        var existing = await unitOfWork.UserSessionRepository
            .FirstOrDefaultAsync(
                x => x.RefreshTokenHash == refreshHash && x.ClientId == clientId,
                true, null, cancellationToken);

        if (existing == null)
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        // Reuse detection: a refresh token whose session is already revoked/expired means it was replayed
        // after rotation — treat it as a compromise and revoke every session the user has.
        if (existing.IsRevoked || existing.ExpiresAt <= now)
        {
            await RevokeAllUserSessionsAsync(existing.UserId, cancellationToken);
            throw new ValidationException(ExceptionsResource.InvalidCredentials);
        }

        // Rotate: revoke the old session (and its access token) and mint a new one, keeping the original
        // absolute expiry so rotation can't extend a session indefinitely.
        existing.Revoke();
        unitOfWork.UserSessionRepository.Update(existing);

        var response = await IssueUserSessionAsync(
            existing.UserId, existing.ClientId, existing.Provider, now, settings,
            refreshExpiry: existing.ExpiresAt, cancellationToken,
            ssoSessionId: existing.SsoSessionId, issuer: request.Issuer);

        await sessionCache.RemoveAsync(existing.TokenHash, cancellationToken);
        return response;
    }

    // Exchanges a PKCE authorization code for a session. Verifies the code is live, was issued to this client
    // and redirect URI, and that the caller holds the matching code verifier (BASE64URL(SHA256(verifier)) ==
    // the stored challenge). The code is single-use.
    private async Task<AuthorizeResponseDto> AuthorizationCodeGrantAsync(
        AuthorizeCommand request, Guid clientId, DateTime now, IAuthOptions settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code)
            || string.IsNullOrWhiteSpace(request.RedirectUri)
            || string.IsNullOrWhiteSpace(request.CodeVerifier))
        {
            throw new ValidationException(ExceptionsResource.InvalidCredentials);
        }

        var codeHash = TokenGenerator.Hash(request.Code);
        var authCode = await unitOfWork.AuthorizationCodeRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.CodeHash == codeHash, cancellationToken);

        if (authCode == null
            || authCode.IsConsumed
            || authCode.IsExpired(now)
            || authCode.ClientId != clientId
            || !string.Equals(authCode.RedirectUri, request.RedirectUri, StringComparison.Ordinal)
            || !string.Equals(PkceHelper.GenerateCodeChallenge(request.CodeVerifier), authCode.CodeChallenge, StringComparison.Ordinal))
        {
            throw new ValidationException(ExceptionsResource.InvalidCredentials);
        }

        // Single-use: spend the code before issuing the session.
        authCode.Consume();
        unitOfWork.AuthorizationCodeRepository.Update(authCode);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Stamp the SSO session the code was minted under onto the issued session, so ending the SSO session
        // (Single Logout) cascades to this app session. Echo the request's nonce into the id_token.
        return await IssueUserSessionAsync(
            authCode.UserId, clientId, provider: null, now, settings,
            refreshExpiry: now.AddDays(settings.RefreshTokenDays), cancellationToken,
            ssoSessionId: authCode.SsoSessionId, issuer: request.Issuer, nonce: authCode.Nonce);
    }

    private async Task<AuthorizeResponseDto> IssueUserSessionAsync(
        Guid? userId, Guid? clientId, string? provider, DateTime now, IAuthOptions settings,
        DateTime refreshExpiry, CancellationToken cancellationToken,
        Guid? ssoSessionId = null, string? issuer = null, string? nonce = null)
    {
        var accessToken = TokenGenerator.Generate();
        var refreshToken = TokenGenerator.Generate();
        var accessExpiry = now.AddMinutes(settings.AccessTokenMinutes);
        var accessHash = TokenGenerator.Hash(accessToken);

        var session = UserSession.Create(
            userId, clientId, provider, accessHash, refreshExpiry, TokenGenerator.Hash(refreshToken), ssoSessionId);

        // The caller is anonymous at /connect — attribute this request's events to the user who just signed in.
        actorEstablisher.Establish(userId, clientId, session.Id);
        await unitOfWork.UserSessionRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // The session is pinned to the user's own tenant (null for a global user) — the gateway trusts this,
        // not a caller-asserted X-Tenant-Id.
        var tenantId = userId is { } owner ? await GetUserTenantAsync(owner, cancellationToken) : null;
        await sessionCache.SetAsync(
            accessHash, new SessionInfo(userId, session.Id, clientId, accessExpiry, tenantId), cancellationToken);

        if (userId is { } uid)
            await EnforceConcurrentSessionCapAsync(uid, settings, now, cancellationToken);

        // OIDC: alongside the opaque access token, issue a signed id_token so relying parties get an
        // offline-verifiable identity assertion. Only for user (not client-credentials) sessions.
        var idToken = await IssueIdTokenAsync(
            userId, clientId, session.Id, ssoSessionId, now, accessExpiry, issuer, nonce, cancellationToken);

        return new AuthorizeResponseDto(accessToken, accessExpiry, "bearer", refreshToken, refreshExpiry, IdToken: idToken);
    }

    // Builds the id_token for a freshly issued user session, when this came through the HTTP token endpoint
    // (issuer known) and names a user. sid ties the token to the SSO session (falling back to the app session)
    // so it correlates with back-/front-channel logout.
    private async Task<string?> IssueIdTokenAsync(
        Guid? userId, Guid? clientId, Guid sessionId, Guid? ssoSessionId, DateTime now, DateTime expiresAt,
        string? issuer, string? nonce, CancellationToken cancellationToken)
    {
        if (userId is not { } uid || string.IsNullOrWhiteSpace(issuer))
            return null;

        var user = await unitOfWork.UserRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == uid, cancellationToken);
        if (user is null)
            return null;

        return idTokenGenerator.Generate(new IdTokenParameters(
            Issuer: issuer!,
            ClientId: clientId ?? Guid.Empty,
            UserId: uid,
            SessionId: ssoSessionId ?? sessionId,
            AuthTime: now,
            ExpiresAt: expiresAt,
            Email: user.Email,
            EmailVerified: user.EmailConfirmed,
            Name: user.UserName,
            Nonce: nonce));
    }

    // Caps how many sessions a user may hold at once: once a new session pushes the count over the configured
    // limit, the oldest live sessions are revoked (and evicted) so only the most recent N survive. Disabled
    // when the limit is <= 0.
    private async Task EnforceConcurrentSessionCapAsync(
        Guid userId, IAuthOptions settings, DateTime now, CancellationToken cancellationToken)
    {
        if (settings.MaxConcurrentSessionsPerUser <= 0)
            return;

        var live = await unitOfWork.UserSessionRepository
            .Query(true, null)
            .Where(x => x.UserId == userId && !x.IsRevoked && x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var excess = live.Skip(settings.MaxConcurrentSessionsPerUser).ToList();
        if (excess.Count == 0)
            return;

        foreach (var session in excess)
            session.Revoke();
        unitOfWork.UserSessionRepository.UpdateRange(excess);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var session in excess)
            await sessionCache.RemoveAsync(session.TokenHash, cancellationToken);
    }

    // Cross-tenant like credential lookup: the request has no proven tenant yet, so bypass the tenant filter.
    private Task<Guid?> GetUserTenantAsync(Guid userId, CancellationToken cancellationToken) =>
        unitOfWork.UserRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .Where(x => x.Id == userId)
            .Select(x => x.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<AuthorizeResponseDto> IssueClientTokenAsync(
        Guid clientId, Guid? tenantId, DateTime now, IAuthOptions settings, CancellationToken cancellationToken)
    {
        // Client-credentials tokens carry no refresh token — the client simply re-authenticates.
        var token = TokenGenerator.Generate();
        var expiry = now.AddDays(settings.RefreshTokenDays);
        var tokenHash = TokenGenerator.Hash(token);

        var session = ClientSession.Create(clientId, tokenHash, expiry);
        actorEstablisher.Establish(null, clientId, session.Id);
        await unitOfWork.ClientSessionRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await sessionCache.SetAsync(tokenHash, new SessionInfo(null, session.Id, clientId, expiry, tenantId), cancellationToken);
        return new AuthorizeResponseDto(token, expiry, "bearer");
    }

    private async Task RevokeAllUserSessionsAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } id) return;

        // Revocation raises a domain event per session that drives cache eviction after commit; just commit here.
        await unitOfWork.UserSessionRepository.RevokeAllForUserAsync(id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public record AuthorizeResponseDto(
    string? AccessToken,
    DateTime? ExpiresAt,
    string TokenType,
    string? RefreshToken = null,
    DateTime? RefreshTokenExpiresAt = null,
    bool TwoFactorRequired = false,
    IReadOnlyCollection<int>? TwoFactorAuthTypeIds = null,
    string? IdToken = null,
    bool TwoFactorAuthenticatorApp = false,
    int? TwoFactorCodeSentVia = null)
{
    /// <summary>
    /// A response that stops short of issuing tokens because the user must complete a second factor. Names the
    /// enrolled 2FA method ids (the user picks Email/SMS/WhatsApp among them), whether an authenticator app can
    /// supply the code, and the method a code was already sent over when there was only one to choose.
    /// </summary>
    public static AuthorizeResponseDto TwoFactorChallenge(
        IReadOnlyCollection<int> twoFactorAuthTypeIds, bool authenticatorApp, int? codeSentVia) =>
        new(AccessToken: null, ExpiresAt: null, TokenType: "bearer",
            TwoFactorRequired: true, TwoFactorAuthTypeIds: twoFactorAuthTypeIds,
            TwoFactorAuthenticatorApp: authenticatorApp, TwoFactorCodeSentVia: codeSentVia);
}
