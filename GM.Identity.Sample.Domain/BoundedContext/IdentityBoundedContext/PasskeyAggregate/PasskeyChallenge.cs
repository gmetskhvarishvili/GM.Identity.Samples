using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate;

/// <summary>
/// A short-lived, single-use challenge issued at the start of a passkey login. The authenticator signs it (via
/// clientDataJSON) and the server matches it on completion, preventing replay. Consumed once used.
/// </summary>
public class PasskeyChallenge : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private PasskeyChallenge() { } // EF Core materialization

    private PasskeyChallenge(Guid userId, string challenge, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Challenge = challenge;
        ExpiresAt = expiresAt;
    }

    public static PasskeyChallenge Create(Guid userId, string challenge, DateTime expiresAt) =>
        new(userId, challenge, expiresAt);

    public Guid UserId { get; private set; }

    /// <summary>The challenge (base64url).</summary>
    public string Challenge { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public bool IsExpired(DateTime now) => ExpiresAt <= now;
}
