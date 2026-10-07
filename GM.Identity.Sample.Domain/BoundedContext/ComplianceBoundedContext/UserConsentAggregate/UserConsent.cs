using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Domain.Identity.UserConsentAggregate.Entities;
using System;

namespace GM.Identity.Sample.Domain.BoundedContext.ComplianceBoundedContext.UserConsentAggregate;

/// <summary>
/// The sample's concrete user-consent aggregate root. Its shape and behaviour live in the GM.Identity base
/// <see cref="GMUserConsent"/>; this type fixes it as the application's aggregate and exposes the factory,
/// mirroring how the other sample entities specialise their GM.Identity bases.
/// </summary>
public class UserConsent : GMUserConsent, IAggregateRoot
{
    private UserConsent() { } // EF Core materialization

    private UserConsent(Guid userId, string consentType, string documentVersion)
        : base(userId, consentType, documentVersion) { }

    public static UserConsent Create(Guid userId, string consentType, string documentVersion) =>
        new(userId, consentType, documentVersion);
}
