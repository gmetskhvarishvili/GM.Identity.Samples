using GM.EntityFramework.Domain.Events;

using System;

namespace GM.Identity.Sample.Domain.Events.Users;

/// <summary>
/// Raised when a user's password is set or changed. Persisted to the domain-event store, it is both the audit
/// record of the change and the source of truth for password-expiry (the newest occurrence is "last changed").
/// </summary>
public sealed record UserPasswordChangedDomainEvent(Guid UserId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
