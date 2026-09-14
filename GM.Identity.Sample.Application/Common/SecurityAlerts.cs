using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.Events.Users;
using GM.Identity.Sample.Domain.SeedWork;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Queues a security-alert notification (via the outbox, like the other user notifications) so the user is told
/// about a security-relevant change to their account — being blocked, locked out, or a password change. No-op
/// when the user has no contact to notify.
/// </summary>
public static class SecurityAlerts
{
    public static async Task QueueSecurityAlertAsync(
        this IUnitOfWork unitOfWork, Guid userId, string? email, string? phoneNumber, string alertType,
        CancellationToken cancellationToken)
    {
        var subject = !string.IsNullOrWhiteSpace(email) ? email : phoneNumber;
        if (string.IsNullOrWhiteSpace(subject))
            return;

        await unitOfWork.OutboxMessageRepository.AddAsync(
            OutboxMessage.From(userId, new SecurityAlertRaisedIntegrationEvent(subject, alertType) { UserId = userId }),
            cancellationToken);
    }
}
