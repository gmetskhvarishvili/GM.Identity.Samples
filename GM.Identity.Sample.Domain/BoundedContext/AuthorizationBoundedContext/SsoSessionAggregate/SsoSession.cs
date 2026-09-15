using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate;

/// <summary>
/// A single sign-on session at the identity provider: the record behind the browser's SSO cookie that proves
/// the user authenticated here. It is what makes cross-application SSO work — once established, subsequent
/// <c>/connect/authorize</c> requests for other clients can be satisfied silently (no re-entering credentials)
/// by presenting the cookie. Only the cookie's hash is stored. Ending the SSO session (logout) revokes it and,
/// via the <c>SsoSessionId</c> stamped on each issued <c>UserSession</c>, cascades to every app session it
/// spawned — Single Logout.
/// </summary>
public class SsoSession : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private SsoSession() // EF Core materialization
    {
    }

    private SsoSession(
        Guid userId, string tokenHash, DateTime authTime, DateTime expiresAt, string? ipAddress, string? userAgent)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        AuthTime = authTime;
        ExpiresAt = expiresAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public static SsoSession Create(
        Guid userId, string tokenHash, DateTime authTime, DateTime expiresAt,
        string? ipAddress = null, string? userAgent = null) =>
        new(userId, tokenHash, authTime, expiresAt, ipAddress, userAgent);

    public Guid UserId { get; private set; }

    /// <summary>Hash of the opaque SSO cookie value (the raw value is never stored).</summary>
    public string TokenHash { get; private set; } = null!;

    /// <summary>When the user last completed primary authentication (for <c>max_age</c> / <c>prompt=login</c>).</summary>
    public DateTime AuthTime { get; private set; }

    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>The session can be used for silent authorization only while it is neither revoked nor expired.</summary>
    public bool IsActiveAt(DateTime now) => !IsRevoked && ExpiresAt > now;

    /// <summary>Ends the SSO session (idempotent). Returns <c>false</c> if it was already revoked.</summary>
    public bool Revoke()
    {
        if (IsRevoked) return false;
        RevokedAt = DateTime.UtcNow;
        return true;
    }
}
