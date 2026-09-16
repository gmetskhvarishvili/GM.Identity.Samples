using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserClientConsentAggregate;

/// <summary>
/// A record that a user granted a client a specific set of OAuth scopes — the durable "remember my choice" that
/// lets a consent-requiring client skip the prompt on the next authorization once the granted scopes cover what
/// is requested. One row per (user, client); re-consent widens the stored scope set.
/// </summary>
public class UserClientConsent : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private UserClientConsent() { } // EF Core materialization

    private UserClientConsent(Guid userId, Guid clientId, string scopes)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ClientId = clientId;
        Scopes = scopes;
        GrantedAt = DateTime.UtcNow;
    }

    public static UserClientConsent Create(Guid userId, Guid clientId, string scopes) =>
        new(userId, clientId, scopes);

    public Guid UserId { get; private set; }
    public Guid ClientId { get; private set; }

    /// <summary>The space-delimited scopes the user has granted this client.</summary>
    public string Scopes { get; private set; } = null!;

    public DateTime GrantedAt { get; private set; }

    public void Grant(string scopes)
    {
        Scopes = scopes;
        GrantedAt = DateTime.UtcNow;
    }
}
