using GM.Exceptions;
using GM.Identity;
using GM.Identity.Sample.Application.Events.Users;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using ValidationException = GM.Exceptions.ValidationException;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Checks the client and user credentials sent to the /connect endpoints. Shared by the token endpoint and the
/// two-factor send-code endpoint, so both apply the same block/lockout rules and failed-attempt counting.
/// </summary>
public static class SignInCredentials
{
    /// <summary>Resolves the active client and verifies its secret.</summary>
    public static async Task<Client> AuthenticateClientAsync(
        this IUnitOfWork unitOfWork, Guid clientId, string? clientSecret, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.Id == clientId && x.IsActive && !x.IsDeleted && !x.IsHidden,
                cancellationToken);

        if (client == null)
            throw new NotFoundException(StringResource.Client, StringResource.Id, clientId);

        if (!PasswordHasher.Verify(clientSecret ?? string.Empty, client.SecretHash, client.SecretSalt))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        return client;
    }

    /// <summary>
    /// Resolves and password-verifies the user (cross-tenant — authentication precedes any tenant context),
    /// applying the block/lockout checks and the failed-attempt counter.
    /// </summary>
    public static async Task<User> AuthenticateUserAsync(
        this IUnitOfWork unitOfWork, string? userName, string? password, DateTime now, IAuthOptions settings,
        CancellationToken cancellationToken)
    {
        // Authentication is a cross-tenant operation: the caller has no proven tenant yet at /connect, so
        // resolve the user by credentials across all tenants (bypass the tenant query filter). The session
        // then carries the user's own tenant, which the gateway enforces on every later request.
        var user = await unitOfWork.UserRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.UserName == userName && x.IsActive && !x.IsDeleted && !x.IsHidden,
                cancellationToken);

        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.UserName, userName ?? string.Empty);

        // Hardening: reject blocked or currently locked-out accounts before checking the password.
        if (user.IsBlocked)
            throw new ValidationException("This account is blocked.");
        if (user.LockoutEnd is { } lockoutEnd && lockoutEnd > now)
            throw new ValidationException("This account is temporarily locked due to failed sign-in attempts.");

        if (!PasswordHasher.Verify(password ?? string.Empty, user.PasswordHash, user.PasswordSalt))
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
}
