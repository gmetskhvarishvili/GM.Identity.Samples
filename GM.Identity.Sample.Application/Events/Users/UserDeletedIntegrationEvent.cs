using GM.Messaging.Domain.Events;
using Wolverine.Attributes;

namespace GM.Identity.Sample.Application.Events.Users;

// Raised when a user is (soft) deleted, so consumers can react to the account going away (revoke downstream
// access, purge projections, etc.). Written to the outbox in the same save as the soft-delete, so the
// notification is durable rather than a best-effort in-process call. UserId is inherited from IntegrationEvent;
// set it via object initializer. MessageIdentity pins the cross-service wire name — must match the consumer alias.
[MessageIdentity("user.deleted")]
public sealed record UserDeletedIntegrationEvent : IntegrationEvent;
