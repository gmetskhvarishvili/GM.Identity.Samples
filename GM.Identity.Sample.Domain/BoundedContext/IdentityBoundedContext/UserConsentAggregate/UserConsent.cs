using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserConsentAggregate;

/// <summary>
/// A record that a user accepted a specific consent document (e.g. Terms of Service, Privacy Policy) at a given
/// version and time — the auditable proof needed for compliance. A new acceptance is recorded per version.
/// </summary>
public class UserConsent : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private UserConsent() { } // EF Core materialization

    private UserConsent(Guid userId, string consentType, string documentVersion)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ConsentType = consentType;
        DocumentVersion = documentVersion;
        AcceptedAt = DateTime.UtcNow;
    }

    public static UserConsent Create(Guid userId, string consentType, string documentVersion) =>
        new(userId, consentType, documentVersion);

    public Guid UserId { get; private set; }

    /// <summary>The consent document accepted (e.g. <c>TermsOfService</c>, <c>PrivacyPolicy</c>).</summary>
    public string ConsentType { get; private set; } = null!;

    /// <summary>The version of the document that was accepted.</summary>
    public string DocumentVersion { get; private set; } = null!;

    public DateTime AcceptedAt { get; private set; }
}
