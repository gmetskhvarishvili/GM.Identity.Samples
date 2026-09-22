using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ConsentDocumentAggregate;

/// <summary>
/// A consent document the application requires users to accept — e.g. Terms of Service or Privacy Policy — as its
/// current version. It is the registry of <em>what</em> must be accepted; a user's actual acceptances are recorded
/// separately (see the UserConsent aggregate). Documents are global (not tenant-scoped) because acceptance is
/// checked at login, before any tenant context exists.
/// </summary>
public class ConsentDocument : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private ConsentDocument() { } // EF Core materialization

    private ConsentDocument(string consentType, string title, string content, string currentVersion, bool isMandatory)
    {
        Id = Guid.NewGuid();
        ConsentType = consentType;
        Title = title;
        Content = content;
        CurrentVersion = currentVersion;
        IsMandatory = isMandatory;
    }

    public static ConsentDocument Create(
        string consentType, string title, string content, string currentVersion, bool isMandatory) =>
        new(consentType, title, content, currentVersion, isMandatory);

    /// <summary>The stable identifier of the document (e.g. <c>TermsOfService</c>, <c>PrivacyPolicy</c>). Immutable.</summary>
    public string ConsentType { get; private set; } = null!;

    /// <summary>Human-readable title shown to the user.</summary>
    public string Title { get; private set; } = null!;

    /// <summary>The document body (or a URL to it).</summary>
    public string Content { get; private set; } = null!;

    /// <summary>The version users must accept. Bumping this makes prior acceptances outstanding again.</summary>
    public string CurrentVersion { get; private set; } = null!;

    /// <summary>When <c>true</c>, an unaccepted current version blocks login until accepted.</summary>
    public bool IsMandatory { get; private set; }

    /// <summary>Updates the mutable fields (everything except the immutable <see cref="ConsentType"/>).</summary>
    public void Update(string title, string content, string currentVersion, bool isMandatory)
    {
        Title = title;
        Content = content;
        CurrentVersion = currentVersion;
        IsMandatory = isMandatory;
    }
}
