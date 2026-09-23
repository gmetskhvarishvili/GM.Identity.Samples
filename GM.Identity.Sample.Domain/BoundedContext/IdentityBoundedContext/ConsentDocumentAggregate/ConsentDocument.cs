using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ConsentDocumentAggregate;

/// <summary>
/// One immutable version of a consent document the application requires users to accept — e.g. Terms of Service
/// or Privacy Policy. Each published version is its own row; exactly one row per <see cref="ConsentType"/> is the
/// current version (<see cref="IsCurrent"/>). Publishing a new version supersedes the previous current row, so the
/// full text of every version is retained (the acceptance records reference the version accepted). Documents are
/// global (not tenant-scoped): acceptance is surfaced after login, which is cross-tenant.
/// </summary>
public class ConsentDocument : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private ConsentDocument() { } // EF Core materialization

    private ConsentDocument(string consentType, string title, string content, string version, bool isMandatory)
    {
        Id = Guid.NewGuid();
        ConsentType = consentType;
        Title = title;
        Content = content;
        Version = version;
        IsMandatory = isMandatory;
        IsCurrent = true;
    }

    /// <summary>Publishes the first version of a new document type (marked current).</summary>
    public static ConsentDocument Create(
        string consentType, string title, string content, string version, bool isMandatory) =>
        new(consentType, title, content, version, isMandatory);

    /// <summary>The document type this row is a version of (e.g. <c>TermsOfService</c>, <c>PrivacyPolicy</c>).</summary>
    public string ConsentType { get; private set; } = null!;

    /// <summary>Human-readable title shown to the user.</summary>
    public string Title { get; private set; } = null!;

    /// <summary>The document body (or a URL to it) for this version. Immutable once published.</summary>
    public string Content { get; private set; } = null!;

    /// <summary>This row's version identifier. Immutable once published.</summary>
    public string Version { get; private set; } = null!;

    /// <summary>When <c>true</c>, an unaccepted current version is reported as pending for the user.</summary>
    public bool IsMandatory { get; private set; }

    /// <summary>Whether this is the current version for its <see cref="ConsentType"/>. Exactly one per type.</summary>
    public bool IsCurrent { get; private set; }

    /// <summary>Marks this version as no longer current (called when a newer version is published).</summary>
    public void Supersede() => IsCurrent = false;
}
