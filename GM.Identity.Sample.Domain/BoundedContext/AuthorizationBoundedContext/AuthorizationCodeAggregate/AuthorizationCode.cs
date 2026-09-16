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
        string? scope, string codeChallenge, string codeChallengeMethod, DateTime expiresAt, Guid? ssoSessionId,
        string? nonce)
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
        SsoSessionId = ssoSessionId;
        Nonce = nonce;
    }

    public static AuthorizationCode Create(
        Guid clientId, Guid userId, string codeHash, string redirectUri,
        string? scope, string codeChallenge, string codeChallengeMethod, DateTime expiresAt,
        Guid? ssoSessionId = null, string? nonce = null) =>
        new(clientId, userId, codeHash, redirectUri, scope, codeChallenge, codeChallengeMethod, expiresAt,
            ssoSessionId, nonce);

    public Guid ClientId { get; private set; }
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public string RedirectUri { get; private set; } = null!;
    public string? Scope { get; private set; }
    public string CodeChallenge { get; private set; } = null!;
    public string CodeChallengeMethod { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    /// <summary>
    /// The SSO session this code was minted under, propagated to the <c>UserSession</c> at token exchange so
    /// Single Logout can cascade. Null when the authorization did not go through an SSO browser session.
    /// </summary>
    public Guid? SsoSessionId { get; private set; }

    /// <summary>The OIDC <c>nonce</c> from the authorization request, echoed into the id_token at exchange.</summary>
    public string? Nonce { get; private set; }

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
