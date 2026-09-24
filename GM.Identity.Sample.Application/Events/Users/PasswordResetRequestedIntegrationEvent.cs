using GM.Messaging.Domain.Events;
using Wolverine.Attributes;

namespace GM.Identity.Sample.Application.Events.Users;

// Raised when a user requests a password reset ("forgot password"), so a downstream notifier (GM.OTP) generates
// and sends a reset code/link to the user's contact (Subject). UserId is inherited from IntegrationEvent; set it
// via object initializer. MessageIdentity pins the cross-service wire name.
[MessageIdentity("user.password.reset.requested")]
public sealed record PasswordResetRequestedIntegrationEvent(
    string Subject,
    int NotificationType) : IntegrationEvent;
