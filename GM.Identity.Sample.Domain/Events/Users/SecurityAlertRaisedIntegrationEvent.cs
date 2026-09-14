using GM.Messaging.Domain.Events;
using Wolverine.Attributes;

namespace GM.Identity.Sample.Domain.Events.Users;

// Raised on a security-relevant account event (blocked, locked out, password changed) so a notification is
// delivered to the user's contact (Subject = email/phone). UserId is inherited from IntegrationEvent; set it
// via object initializer. MessageIdentity pins the cross-service wire name for the notification producer.
[MessageIdentity("user.security.alert")]
public sealed record SecurityAlertRaisedIntegrationEvent(
    string Subject,
    string AlertType) : IntegrationEvent;

/// <summary>Well-known <see cref="SecurityAlertRaisedIntegrationEvent.AlertType"/> values.</summary>
public static class SecurityAlertTypes
{
    public const string AccountBlocked = "AccountBlocked";
    public const string AccountLockedOut = "AccountLockedOut";
    public const string PasswordChanged = "PasswordChanged";
}
