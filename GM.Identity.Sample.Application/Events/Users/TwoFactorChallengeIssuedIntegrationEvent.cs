using GM.Messaging.Domain.Events;
using Wolverine.Attributes;

namespace GM.Identity.Sample.Application.Events.Users;

// Raised when a 2FA login needs a one-time code: the OTP service generates it under Subject (the key the
// two_factor grant verifies against — the user's email, falling back to the phone) and sends it to Destination
// over Channel (0 = Email, 1 = SMS, 2 = WhatsApp — the numbering Notifications delivers by). UserId is
// inherited from IntegrationEvent; set it via object initializer. MessageIdentity pins the cross-service wire
// name so the OTP producer (which owns its own copy of this contract) resolves the same Wolverine message-type.
[MessageIdentity("user.twofactor.challenge")]
public sealed record TwoFactorChallengeIssuedIntegrationEvent(
    string Subject,
    string Destination,
    int Channel) : IntegrationEvent;
