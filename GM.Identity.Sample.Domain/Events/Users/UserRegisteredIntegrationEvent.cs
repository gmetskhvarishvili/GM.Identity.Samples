using GM.Messaging.Domain.Events;
using Wolverine.Attributes;

namespace GM.Identity.Sample.Domain.Events.Users;

// UserId is inherited from IntegrationEvent and must be set via object initializer
// (a positional parameter matching the inherited init-only property is silently dropped).
// MessageIdentity sets the cross-service wire name so consumers with their own copy of this
// contract (different namespace) resolve the same Wolverine message-type. Must match the alias
// on the consumer side.
[MessageIdentity("user.registered")]
public sealed record UserRegisteredIntegrationEvent(
    string? Email,
    string? Username,
    string? PhoneNumber) : IntegrationEvent;
