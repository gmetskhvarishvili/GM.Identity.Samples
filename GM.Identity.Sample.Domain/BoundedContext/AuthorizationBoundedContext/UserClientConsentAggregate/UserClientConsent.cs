using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Domain.Authorization.UserClientConsentAggregate.Entities;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserClientConsentAggregate;

/// <summary>
/// The sample's concrete per-client consent aggregate root. Its shape and behaviour live in the GM.Identity
/// base <see cref="GMUserClientConsent"/>; this type fixes it as the application's aggregate and exposes the
/// factory, mirroring how the other sample entities specialise their GM.Identity bases.
/// </summary>
public class UserClientConsent : GMUserClientConsent, IAggregateRoot
{
    private UserClientConsent() { } // EF Core materialization

    private UserClientConsent(Guid userId, Guid clientId, string scopes) : base(userId, clientId, scopes) { }

    public static UserClientConsent Create(Guid userId, Guid clientId, string scopes) =>
        new(userId, clientId, scopes);
}
