using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common;
using GM.Identity;
using GM.Identity.Authorization;
using GM.Identity.Oidc;
using GM.Identity.Sample.Application.Infrastructure.Services.OTP;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.DeviceCodeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Application.Events.Users;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
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

    /// <summary>The device code, supplied with the device-authorization grant while polling the token endpoint.</summary>
    public string? DeviceCode { get; set; }
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
    IIdTokenGenerator idTokenGenerator,
    IAuthOptions options) : IRequestHandler<AuthorizeCommand, AuthorizeResponseDto>
{
    public async Task<AuthorizeResponseDto> Handle(AuthorizeCommand request, CancellationToken cancellationToken)
    {
        // Every grant authenticates the client first.
        var client = await unitOfWork.ClientRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.Id == request.ClientId && x.IsActive && !x.IsDeleted && !x.IsHidden,
                cancellationToken);

        if (client == null)
            throw new NotFoundException(StringResource.Client, StringResource.Id, request.ClientId);

        if (!PasswordHasher.Verify(request.ClientSecret, client.SecretHash, client.SecretSalt))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        var settings = options;
        var now = DateTime.UtcNow;

        return request.GrantType switch
        {
            "ClientCredentials" => await IssueClientTokenAsync(client.Id, now, settings, cancellationToken),
            "refresh_token" => await RefreshAsync(request, client.Id, now, settings, cancellationToken),
            "two_factor" => await TwoFactorGrantAsync(request, client.Id, now, settings, cancellationToken),
            "authorization_code" => await AuthorizationCodeGrantAsync(request, client.Id, now, settings, cancellationToken),
            "urn:ietf:params:oauth:grant-type:device_code" => await DeviceCodeGrantAsync(request, client.Id, now, settings, cancellationToken),
            _ => await PasswordGrantAsync(request, client.Id, now, settings, cancellationToken),
        };
    }

    private async Task<AuthorizeResponseDto> PasswordGrantAsync(
        AuthorizeCommand request, Guid clientId, DateTime now, IAuthOptions settings, CancellationToken cancellationToken)
    {
        var user = await AuthenticateUserAsync(request, now, settings, cancellationToken);

        // Two-factor step-up: if the user has any confirmed second factor — a contact OTP method or an
        // authenticator-app (TOTP) device — the password alone does not mint a session. For contact methods we
        // send the one-time code (via the outbox); TOTP needs no send (the app generates it). Return a
        // challenge; the caller completes login with grant_type=two_factor + the code.
        var twoFactorTypeIds = await GetEnrolledTwoFactorTypeIdsAsync(user.Id, cancellationToken);
        var hasTotp = await GetConfirmedTotpSecretAsync(user.Id, cancellationToken) != null;
        if (twoFactorTypeIds.Count > 0 || hasTotp)
        {
            var subject = TwoFactorSubject(user);
            if (twoFactorTypeIds.Count > 0 && !string.IsNullOrWhiteSpace(subject))
            {
                await unitOfWork.OutboxMessageRepository.AddAsync(
                    OutboxMessage.From(user.Id,
                        new TwoFactorChallengeIssuedIntegrationEvent(subject) { UserId = user.Id }),
                    cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return AuthorizeResponseDto.TwoFactorChallenge(twoFactorTypeIds);
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

        var user = await AuthenticateUserAsync(request, now, settings, cancellationToken);

        var twoFactorTypeIds = await GetEnrolledTwoFactorTypeIdsAsync(user.Id, cancellationToken);
        var totpSecret = await GetConfirmedTotpSecretAsync(user.Id, cancellationToken);
        if (twoFactorTypeIds.Count == 0 && totpSecret == null)
            throw new ValidationException("This account has no second factor configured.");

        // The submitted value can be an authenticator-app (TOTP) code or the one-time code sent to the user's
        // contact. Try each in turn; only if none matches is the second factor rejected.
        var subject = TwoFactorSubject(user);
        var verified =
            (totpSecret != null && Totp.Verify(totpSecret, request.Code))
            || (twoFactorTypeIds.Count > 0 && !string.IsNullOrWhiteSpace(subject)
                && await TryVerifyOtpAsync(subject, request.Code, cancellationToken));

        if (!verified)
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        return await IssueUserSessionAsync(
            user.Id, clientId, provider: null, now, settings,
            refreshExpiry: now.AddDays(settings.RefreshTokenDays), cancellationToken, issuer: request.Issuer);
    }

    // Resolves and password-verifies the user (cross-tenant — authentication precedes any tenant context),
    // applying the block/lockout checks and the failed-attempt counter. Shared by the password and
    // two-factor grants.
    private async Task<User> AuthenticateUserAsync(
        AuthorizeCommand request, DateTime now, IAuthOptions settings, CancellationToken cancellationToken)
    {
        // Authentication is a cross-tenant operation: the caller has no proven tenant yet at /connect, so
        // resolve the user by credentials across all tenants (bypass the tenant query filter). The tenant
        // the caller then acts under is asserted per request via the X-Tenant-Id header.
        var user = await unitOfWork.UserRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.UserName == request.UserName && x.IsActive && !x.IsDeleted && !x.IsHidden,
                cancellationToken);

        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.UserName, request.UserName ?? string.Empty);

        // Hardening: reject blocked or currently locked-out accounts before checking the password.
        if (user.IsBlocked)
            throw new ValidationException("This account is blocked.");
        if (user.LockoutEnd is { } lockoutEnd && lockoutEnd > now)
            throw new ValidationException("This account is temporarily locked due to failed sign-in attempts.");

        if (!PasswordHasher.Verify(request.Password ?? string.Empty, user.PasswordHash, user.PasswordSalt))
        {
            // Count the failure and lock the account once the threshold is reached (ensure lockout is
            // enabled first, since IncreaseAccessFailedCount only sets LockoutEnd when it is).
            user.EnableLockOut();
            var willLock = user.AccessFailedCount + 1 >= settings.MaxFailedAccessAttempts;
            user.IncreaseAccessFailedCount(willLock, now.AddMinutes(settings.LockoutMinutes));
            unitOfWork.UserRepository.Update(user);

            // Alert the user when this failure actually trips the lockout (possible break-in attempt).
            if (willLock)
                await unitOfWork.QueueSecurityAlertAsync(
                    user.Id, user.Email, user.PhoneNumber, SecurityAlertTypes.AccountLockedOut, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ValidationException(ExceptionsResource.InvalidCredentials);
        }

        return user;
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

    // The payload the one-time code is issued/validated against — the email, falling back to the phone.
    private static string? TwoFactorSubject(User user) =>
        !string.IsNullOrWhiteSpace(user.Email) ? user.Email : user.PhoneNumber;

    // The 2FA methods the user is currently enrolled in AND has confirmed (active, non-deleted). Empty when
    // the user has no confirmed second factor, in which case login proceeds straight to issuing a session.
    // A pending (unconfirmed) enrolment deliberately does not gate login, so a user who cannot complete setup
    // is never locked out.
    private async Task<IReadOnlyCollection<int>> GetEnrolledTwoFactorTypeIdsAsync(
        Guid userId, CancellationToken cancellationToken) =>
        await unitOfWork.UserTwoFactorAuthTypeRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.IsConfirmed && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.TwoFactorAuthTypeId)
            .ToListAsync(cancellationToken);

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

    // Device Authorization Grant polling (RFC 8628 §3.4/§3.5): the browserless device exchanges its device_code
    // for tokens once the user has approved. Returns the RFC error codes as validation failures while the device
    // keeps polling — authorization_pending, slow_down (polled too fast), access_denied, expired_token.
    private async Task<AuthorizeResponseDto> DeviceCodeGrantAsync(
        AuthorizeCommand request, Guid clientId, DateTime now, IAuthOptions settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceCode))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        var hash = TokenGenerator.Hash(request.DeviceCode);
        var deviceCode = await unitOfWork.DeviceCodeRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.DeviceCodeHash == hash, cancellationToken);

        // Unknown or wrong-client device code → treat as expired (never leak which).
        if (deviceCode == null || deviceCode.ClientId != clientId)
            throw new ValidationException("expired_token");

        if (deviceCode.IsExpired(now))
        {
            unitOfWork.DeviceCodeRepository.Remove(deviceCode);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ValidationException("expired_token");
        }

        if (deviceCode.Status == DeviceCodeStatus.Denied)
            throw new ValidationException("access_denied");

        var tooFast = deviceCode.RegisterPollAndCheckTooFast(now);

        if (deviceCode.Status == DeviceCodeStatus.Approved && deviceCode.UserId is { } userId)
        {
            // Single-use: spend the device code before issuing the session.
            unitOfWork.DeviceCodeRepository.Remove(deviceCode);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await IssueUserSessionAsync(
                userId, clientId, provider: "device", now, settings,
                refreshExpiry: now.AddDays(settings.RefreshTokenDays), cancellationToken, issuer: request.Issuer);
        }

        // Still pending — persist the poll timestamp and tell the device to keep (or slow) polling.
        unitOfWork.DeviceCodeRepository.Update(deviceCode);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        throw new ValidationException(tooFast ? "slow_down" : "authorization_pending");
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

        await unitOfWork.UserSessionRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await sessionCache.SetAsync(
            accessHash, new SessionInfo(userId, session.Id, clientId, accessExpiry), cancellationToken);

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

    private async Task<AuthorizeResponseDto> IssueClientTokenAsync(
        Guid clientId, DateTime now, IAuthOptions settings, CancellationToken cancellationToken)
    {
        // Client-credentials tokens carry no refresh token — the client simply re-authenticates.
        var token = TokenGenerator.Generate();
        var expiry = now.AddDays(settings.RefreshTokenDays);
        var tokenHash = TokenGenerator.Hash(token);

        var session = ClientSession.Create(clientId, tokenHash, expiry);
        await unitOfWork.ClientSessionRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await sessionCache.SetAsync(tokenHash, new SessionInfo(null, session.Id, clientId, expiry), cancellationToken);
        return new AuthorizeResponseDto(token, expiry, "bearer");
    }

    private async Task RevokeAllUserSessionsAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } id) return;

        // Revocation queues a SessionsRevoked outbox message for reliable cache eviction; just commit here.
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
    string? IdToken = null)
{
    /// <summary>
    /// A response that stops short of issuing tokens because the user must complete a second factor. Names
    /// the enrolled 2FA method ids so the caller knows which challenge to present.
    /// </summary>
    public static AuthorizeResponseDto TwoFactorChallenge(IReadOnlyCollection<int> twoFactorAuthTypeIds) =>
        new(AccessToken: null, ExpiresAt: null, TokenType: "bearer",
            TwoFactorRequired: true, TwoFactorAuthTypeIds: twoFactorAuthTypeIds);
}
