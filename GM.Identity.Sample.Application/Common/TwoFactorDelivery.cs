using GM.Identity.Sample.Application.Events.Users;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.Enums;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using ValidationException = GM.Exceptions.ValidationException;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Second-factor enrolment lookups and code delivery for the login flow. The code goes out through the outbox as
/// <see cref="TwoFactorChallengeIssuedIntegrationEvent"/>: the OTP service generates it and Notifications sends it
/// to the destination over the chosen channel.
/// </summary>
public static class TwoFactorDelivery
{
    /// <summary>
    /// The 2FA methods the user is currently enrolled in (active, non-deleted). Empty when the user has no second
    /// factor. Enrolment activates a method immediately, so any active enrolment gates login.
    /// </summary>
    public static async Task<IReadOnlyCollection<int>> GetEnrolledTwoFactorTypeIdsAsync(
        this IUnitOfWork unitOfWork, Guid userId, CancellationToken cancellationToken) =>
        await unitOfWork.UserTwoFactorAuthTypeRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.TwoFactorAuthTypeId)
            .ToListAsync(cancellationToken);

    /// <summary>The enrolled methods that can deliver a code (Email, SMS, WhatsApp).</summary>
    public static IReadOnlyCollection<TwoFactorMethod> DeliveryMethods(IEnumerable<int> enrolledTypeIds) =>
        enrolledTypeIds
            .Where(id => Enum.IsDefined(typeof(TwoFactorMethod), id))
            .Select(id => (TwoFactorMethod)id)
            .ToList();

    /// <summary>
    /// The key the one-time code is issued and verified under: the email, falling back to the phone. It stays the
    /// same whichever channel delivers the code, so the two_factor grant verifies it without knowing the channel.
    /// </summary>
    public static string? TwoFactorSubject(User user) =>
        !string.IsNullOrWhiteSpace(user.Email) ? user.Email : user.PhoneNumber;

    /// <summary>
    /// Queues a one-time code for <paramref name="user"/> over <paramref name="method"/> and commits. Throws when
    /// the user has no contact for that method (no email for Email, no phone number for SMS/WhatsApp).
    /// </summary>
    public static async Task QueueTwoFactorCodeAsync(
        this IUnitOfWork unitOfWork, User user, TwoFactorMethod method, CancellationToken cancellationToken)
    {
        var destination = method.UsesPhoneNumber() ? user.PhoneNumber : user.Email;
        if (string.IsNullOrWhiteSpace(destination))
            throw new ValidationException(method.UsesPhoneNumber()
                ? $"This account has no phone number to send a {method} code to."
                : "This account has no email address to send a code to.");

        var subject = TwoFactorSubject(user)!; // non-null: the destination above is one of the two contacts

        await unitOfWork.OutboxMessageRepository.AddAsync(
            OutboxMessage.From(user.Id,
                new TwoFactorChallengeIssuedIntegrationEvent(subject, destination, (int)method.ToChannel())
                {
                    UserId = user.Id,
                }),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
