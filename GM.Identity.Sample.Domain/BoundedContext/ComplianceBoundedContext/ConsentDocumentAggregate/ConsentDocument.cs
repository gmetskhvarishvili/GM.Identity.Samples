using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Domain.Identity.ConsentDocumentAggregate.Entities;

namespace GM.Identity.Sample.Domain.BoundedContext.ComplianceBoundedContext.ConsentDocumentAggregate;

/// <summary>
/// The sample's concrete consent-document aggregate root. Its shape and behaviour live in the GM.Identity base
/// <see cref="GMConsentDocument"/>; this type fixes it as the application's aggregate and exposes the factory,
/// mirroring how the other sample entities specialise their GM.Identity bases.
/// </summary>
public class ConsentDocument : GMConsentDocument, IAggregateRoot
{
    private ConsentDocument() { } // EF Core materialization

    private ConsentDocument(string consentType, string title, string content, string version, bool isMandatory)
        : base(consentType, title, content, version, isMandatory) { }

    /// <summary>Publishes the first version of a new document type (marked current).</summary>
    public static ConsentDocument Create(
        string consentType, string title, string content, string version, bool isMandatory) =>
        new(consentType, title, content, version, isMandatory);
}
