using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ApiKeyAggregate;

/// <summary>
/// A long-lived personal access token (API key) issued to a user for non-interactive access. Only the hash is
/// stored; the plaintext is shown once at creation. Optionally expires, and records when it was last used.
/// </summary>
public class ApiKey : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private ApiKey() { } // EF Core materialization

    private ApiKey(Guid userId, string name, string keyHash, DateTime? expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Name = name;
        KeyHash = keyHash;
        ExpiresAt = expiresAt;
    }

    public static ApiKey Create(Guid userId, string name, string keyHash, DateTime? expiresAt) =>
        new(userId, name, keyHash, expiresAt);

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string KeyHash { get; private set; } = null!;
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    public bool IsExpired(DateTime now) => ExpiresAt is { } e && e <= now;

    /// <summary>Stamps the key as used now (called when it authenticates a request).</summary>
    public void MarkUsed() => LastUsedAt = DateTime.UtcNow;
}
