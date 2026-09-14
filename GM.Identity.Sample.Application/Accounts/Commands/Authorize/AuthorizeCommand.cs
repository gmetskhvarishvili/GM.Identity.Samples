using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Infrastructure.Services.OTP;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.Events.Users;
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
    IOptions<AuthOptions> options) : IRequestHandler<AuthorizeCommand, AuthorizeResponseDto>
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

        var settings = options.Value;
        var now = DateTime.UtcNow;

        return request.GrantType switch
        {
            "ClientCredentials" => await IssueClientTokenAsync(client.Id, now, settings, cancellationToken),
            "refresh_token" => await RefreshAsync(request, client.Id, now, settings, cancellationToken),
            "two_factor" => await TwoFactorGrantAsync(request, client.Id, now, settings, cancellationToken),
            _ => await PasswordGrantAsync(request, client.Id, now, settings, cancellationToken),
        };
    }

    private async Task<AuthorizeResponseDto> PasswordGrantAsync(
        AuthorizeCommand request, Guid clientId, DateTime now, AuthOptions settings, CancellationToken cancellationToken)
    {
        var user = await AuthenticateUserAsync(request, now, settings, cancellationToken);

        // Two-factor step-up: if the user is enrolled in any 2FA method, the password alone does not mint a
        // session. Send the one-time code (via the outbox, like contact confirmation) and return a challenge
        // naming the enrolled methods; the caller completes login with grant_type=two_factor + the code.
        var twoFactorTypeIds = await GetEnrolledTwoFactorTypeIdsAsync(user.Id, cancellationToken);
        if (twoFactorTypeIds.Count > 0)
        {
            var subject = TwoFactorSubject(user);
            if (!string.IsNullOrWhiteSpace(subject))
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
            refreshExpiry: now.AddDays(settings.RefreshTokenDays), cancellationToken);
    }

    private async Task<AuthorizeResponseDto> TwoFactorGrantAsync(
        AuthorizeCommand request, Guid clientId, DateTime now, AuthOptions settings, CancellationToken cancellationToken)
    {
        // Completing a 2FA login: re-verify the password (the challenge is not itself a bearer of trust),
        // require that the user really is enrolled, then validate the one-time code before issuing a session.
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        var user = await AuthenticateUserAsync(request, now, settings, cancellationToken);

        var twoFactorTypeIds = await GetEnrolledTwoFactorTypeIdsAsync(user.Id, cancellationToken);
        if (twoFactorTypeIds.Count == 0)
            throw new ValidationException("This account has no second factor configured.");

        var subject = TwoFactorSubject(user);
        if (string.IsNullOrWhiteSpace(subject))
            throw new ValidationException("This account has no contact to validate the second factor against.");

        // The submitted value is either the one-time code sent to the user's contact, or one of their
        // single-use backup codes. Try the OTP first; if it doesn't validate, fall back to consuming a
        // recovery code. Only if neither matches is the second factor rejected.
        if (!await TryVerifyOtpAsync(subject, request.Code, cancellationToken)
            && !await TryConsumeRecoveryCodeAsync(user.Id, request.Code, cancellationToken))
        {
            throw new ValidationException(ExceptionsResource.InvalidCredentials);
        }

        return await IssueUserSessionAsync(
            user.Id, clientId, provider: null, now, settings,
            refreshExpiry: now.AddDays(settings.RefreshTokenDays), cancellationToken);
    }

    // Resolves and password-verifies the user (cross-tenant — authentication precedes any tenant context),
    // applying the block/lockout checks and the failed-attempt counter. Shared by the password and
    // two-factor grants.
    private async Task<User> AuthenticateUserAsync(
        AuthorizeCommand request, DateTime now, AuthOptions settings, CancellationToken cancellationToken)
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
            return false; // Not a valid OTP — the caller will try a recovery code.
        }
    }

    // Spends a single-use recovery code if the submitted value matches an unused one. Cross-tenant, like the
    // rest of the login path (the caller has no proven tenant yet).
    private async Task<bool> TryConsumeRecoveryCodeAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        var hash = TokenGenerator.Hash(code);
        var recoveryCode = await unitOfWork.UserRecoveryCodeRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.CodeHash == hash && x.UsedAt == null
                     && x.IsActive && !x.IsDeleted && !x.IsHidden,
                cancellationToken);

        if (recoveryCode == null || !recoveryCode.Consume())
            return false;

        unitOfWork.UserRecoveryCodeRepository.Update(recoveryCode);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
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

    private async Task<AuthorizeResponseDto> RefreshAsync(
        AuthorizeCommand request, Guid clientId, DateTime now, AuthOptions settings, CancellationToken cancellationToken)
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
            refreshExpiry: existing.ExpiresAt, cancellationToken);

        await sessionCache.RemoveAsync(existing.TokenHash, cancellationToken);
        return response;
    }

    private async Task<AuthorizeResponseDto> IssueUserSessionAsync(
        Guid? userId, Guid? clientId, string? provider, DateTime now, AuthOptions settings,
        DateTime refreshExpiry, CancellationToken cancellationToken)
    {
        var accessToken = TokenGenerator.Generate();
        var refreshToken = TokenGenerator.Generate();
        var accessExpiry = now.AddMinutes(settings.AccessTokenMinutes);
        var accessHash = TokenGenerator.Hash(accessToken);

        var session = UserSession.Create(
            userId, clientId, provider, accessHash, refreshExpiry, TokenGenerator.Hash(refreshToken));

        await unitOfWork.UserSessionRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await sessionCache.SetAsync(
            accessHash, new SessionInfo(userId, session.Id, clientId, accessExpiry), cancellationToken);

        return new AuthorizeResponseDto(accessToken, accessExpiry, "bearer", refreshToken, refreshExpiry);
    }

    private async Task<AuthorizeResponseDto> IssueClientTokenAsync(
        Guid clientId, DateTime now, AuthOptions settings, CancellationToken cancellationToken)
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

        var sessions = await unitOfWork.UserSessionRepository
            .Query(true, null)
            .Where(x => x.UserId == id && !x.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke();
            await sessionCache.RemoveAsync(session.TokenHash, cancellationToken);
        }

        unitOfWork.UserSessionRepository.UpdateRange(sessions);
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
    IReadOnlyCollection<int>? TwoFactorAuthTypeIds = null)
{
    /// <summary>
    /// A response that stops short of issuing tokens because the user must complete a second factor. Names
    /// the enrolled 2FA method ids so the caller knows which challenge to present.
    /// </summary>
    public static AuthorizeResponseDto TwoFactorChallenge(IReadOnlyCollection<int> twoFactorAuthTypeIds) =>
        new(AccessToken: null, ExpiresAt: null, TokenType: "bearer",
            TwoFactorRequired: true, TwoFactorAuthTypeIds: twoFactorAuthTypeIds);
}
