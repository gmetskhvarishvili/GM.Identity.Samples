using GM.Messaging.Domain.Events;
using Wolverine.Attributes;

namespace GM.Identity.Sample.Domain.Events.Users;

// UserId is inherited from IntegrationEvent; set it via object initializer, not positionally.
// MessageIdentity sets the cross-service wire name so consumers with their own copy of this
// contract (different namespace) resolve the same Wolverine message-type. Must match the alias
// on the consumer side.
[MessageIdentity("user.confirmed")]
public sealed record UserConfirmedIntegrationEvent(
    string? Subject,
    int ConfirmationType) : IntegrationEvent;
