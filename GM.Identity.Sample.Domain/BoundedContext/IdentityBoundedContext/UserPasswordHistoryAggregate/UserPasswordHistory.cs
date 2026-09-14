using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPasswordHistoryAggregate;

/// <summary>
/// A past password hash for a user, retained so a password change can be rejected if it reuses a recent one.
/// Stores only the hash + salt (never plaintext), ordered by when it was set.
/// </summary>
public class UserPasswordHistory : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private UserPasswordHistory() // EF Core materialization
    {
    }

    private UserPasswordHistory(Guid userId, string passwordHash, string passwordSalt, DateTime setAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        SetAt = setAt;
    }

    public static UserPasswordHistory Create(Guid userId, string passwordHash, string passwordSalt, DateTime setAt) =>
        new(userId, passwordHash, passwordSalt, setAt);

    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = null!;
    public string PasswordSalt { get; private set; } = null!;

    /// <summary>When this password was set (used to keep only the most recent N).</summary>
    public DateTime SetAt { get; private set; }
}
