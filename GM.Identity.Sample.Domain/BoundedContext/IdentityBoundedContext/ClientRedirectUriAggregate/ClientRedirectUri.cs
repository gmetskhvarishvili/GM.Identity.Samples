using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Domain.Identity.ClientRedirectUriAggregate.Entities;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate;

/// <summary>
/// The sample's concrete client-redirect-URI aggregate root. Its shape lives in the GM.Identity base
/// <see cref="GMClientRedirectUri"/>; this type fixes it as the application's aggregate and exposes the factory,
/// mirroring how the other sample entities specialise their GM.Identity bases.
/// </summary>
public class ClientRedirectUri : GMClientRedirectUri, IAggregateRoot
{
    private ClientRedirectUri() { } // EF Core materialization

    private ClientRedirectUri(Guid clientId, string uri) : base(clientId, uri) { }

    public static ClientRedirectUri Create(Guid clientId, string uri) => new(clientId, uri);
}
