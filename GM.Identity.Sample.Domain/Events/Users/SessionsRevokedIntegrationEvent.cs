using GM.Messaging.Domain.Events;
using Wolverine.Attributes;

using System.Collections.Generic;

namespace GM.Identity.Sample.Domain.Events.Users;

// Raised when a user's active sessions are revoked (block, deactivate, delete, password change, or session-cap
// eviction) so a consumer reliably evicts the corresponding session-cache entries. Written to the outbox in the
// same transaction as the revocation, so the eviction is durable rather than a best-effort in-process call.
// UserId is inherited from IntegrationEvent; set it via object initializer. MessageIdentity pins the wire name.
[MessageIdentity("user.sessions.revoked")]
public sealed record SessionsRevokedIntegrationEvent(
    IReadOnlyCollection<string> TokenHashes) : IntegrationEvent;
