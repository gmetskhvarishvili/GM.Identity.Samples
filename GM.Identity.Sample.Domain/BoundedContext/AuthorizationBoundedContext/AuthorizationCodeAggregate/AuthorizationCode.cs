using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate;

/// <summary>
/// A short-lived, single-use OAuth authorization code minted by <c>/connect/authorize</c> and exchanged for a
/// session at the token endpoint. Only the code's hash is stored. It is bound to the client, the redirect URI,
/// and (for PKCE) the code challenge, so the exchange must present a matching redirect URI and code verifier.
/// </summary>
public class AuthorizationCode : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private AuthorizationCode() // EF Core materialization
    {
    }

    private AuthorizationCode(
        Guid clientId, Guid userId, string codeHash, string redirectUri,
        string? scope, string codeChallenge, string codeChallengeMethod, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        UserId = userId;
        CodeHash = codeHash;
        RedirectUri = redirectUri;
        Scope = scope;
        CodeChallenge = codeChallenge;
        CodeChallengeMethod = codeChallengeMethod;
        ExpiresAt = expiresAt;
    }

    public static AuthorizationCode Create(
        Guid clientId, Guid userId, string codeHash, string redirectUri,
        string? scope, string codeChallenge, string codeChallengeMethod, DateTime expiresAt) =>
        new(clientId, userId, codeHash, redirectUri, scope, codeChallenge, codeChallengeMethod, expiresAt);

    public Guid ClientId { get; private set; }
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public string RedirectUri { get; private set; } = null!;
    public string? Scope { get; private set; }
    public string CodeChallenge { get; private set; } = null!;
    public string CodeChallengeMethod { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    public bool IsConsumed => ConsumedAt.HasValue;
    public bool IsExpired(DateTime now) => ExpiresAt <= now;

    /// <summary>Spends the code (idempotent). Returns <c>false</c> if it was already used.</summary>
    public bool Consume()
    {
        if (IsConsumed) return false;
        ConsumedAt = DateTime.UtcNow;
        return true;
    }
}
