using GM.Messaging.Domain.Events;
using Wolverine.Attributes;

namespace GM.Identity.Sample.Application.Events.Users;

// Raised when a password login hits a 2FA-enrolled user: the second-factor one-time code should be
// generated and delivered to Subject (the user's email/phone). UserId is inherited from IntegrationEvent;
// set it via object initializer. MessageIdentity pins the cross-service wire name so the OTP producer
// (which owns its own copy of this contract) resolves the same Wolverine message-type.
[MessageIdentity("user.twofactor.challenge")]
public sealed record TwoFactorChallengeIssuedIntegrationEvent(
    string Subject) : IntegrationEvent;
