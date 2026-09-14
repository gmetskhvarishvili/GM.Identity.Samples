using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPasswordHistoryAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Password-reuse prevention: rejects a new password that matches the current one or any of the most recent
/// <see cref="HistoryDepth"/> retained hashes, and records each new password so future changes can be checked.
/// Only hashes are compared/stored — never plaintext.
/// </summary>
public static class PasswordHistory
{
    /// <summary>How many previous passwords (plus the current one) a new password is checked against.</summary>
    public const int HistoryDepth = 5;

    /// <summary>
    /// Throws <see cref="ValidationException"/> if <paramref name="newPassword"/> matches the current password
    /// or any recent history entry for the user.
    /// </summary>
    public static async Task EnsureNotReusedAsync(
        this IUnitOfWork unitOfWork, Guid userId, string newPassword,
        string currentHash, string currentSalt, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(currentHash) && PasswordHasher.Verify(newPassword, currentHash, currentSalt))
            throw new ValidationException("The new password must be different from the current password.");

        var recent = await unitOfWork.UserPasswordHistoryRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .OrderByDescending(x => x.SetAt)
            .Take(HistoryDepth)
            .ToListAsync(cancellationToken);

        if (recent.Any(h => PasswordHasher.Verify(newPassword, h.PasswordHash, h.PasswordSalt)))
            throw new ValidationException($"The new password cannot match any of your last {HistoryDepth} passwords.");
    }

    /// <summary>
    /// Records a newly set password hash in the user's history and prunes anything beyond the retained depth.
    /// The caller saves the unit of work.
    /// </summary>
    public static async Task RecordAsync(
        this IUnitOfWork unitOfWork, Guid userId, string passwordHash, string passwordSalt, CancellationToken cancellationToken)
    {
        await unitOfWork.UserPasswordHistoryRepository.AddAsync(
            UserPasswordHistory.Create(userId, passwordHash, passwordSalt, DateTime.UtcNow), cancellationToken);

        // Prune entries beyond the retained depth (keep the newest HistoryDepth).
        var stale = await unitOfWork.UserPasswordHistoryRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.SetAt)
            .Skip(HistoryDepth)
            .ToListAsync(cancellationToken);
        if (stale.Count > 0)
            unitOfWork.UserPasswordHistoryRepository.RemoveRange(stale);
    }
}
