using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserRecoveryCodeAggregate;

/// <summary>
/// A single-use backup code a user can present in place of their normal second factor. Only the hash is
/// stored — the plaintext is shown to the user once at generation and is never recoverable. A code is spent
/// the first time it is used.
/// </summary>
public class UserRecoveryCode : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private UserRecoveryCode() // EF Core materialization
    {
    }

    private UserRecoveryCode(Guid userId, string codeHash)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CodeHash = codeHash;
    }

    public static UserRecoveryCode Create(Guid userId, string codeHash) => new(userId, codeHash);

    public Guid UserId { get; private set; }

    /// <summary>Hash of the recovery code (never the plaintext).</summary>
    public string CodeHash { get; private set; } = null!;

    /// <summary>When the code was consumed, or <c>null</c> while it is still usable.</summary>
    public DateTime? UsedAt { get; private set; }

    /// <summary>Whether the code has already been spent.</summary>
    public bool IsUsed => UsedAt.HasValue;

    /// <summary>Spends the code (idempotent). Returns <c>false</c> if it was already used.</summary>
    public bool Consume()
    {
        if (IsUsed) return false;
        UsedAt = DateTime.UtcNow;
        return true;
    }
}
