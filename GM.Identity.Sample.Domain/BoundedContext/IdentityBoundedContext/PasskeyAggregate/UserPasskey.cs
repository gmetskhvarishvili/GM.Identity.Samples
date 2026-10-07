using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Domain.Identity.UserPasskeyAggregate.Entities;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate;

/// <summary>
/// The sample's concrete WebAuthn/FIDO2 passkey aggregate root. Its shape and behaviour live in the GM.Identity
/// base <see cref="GMUserPasskey"/>; this type fixes it as the application's aggregate and exposes the factory,
/// mirroring how the other sample entities specialise their GM.Identity bases.
/// </summary>
public class UserPasskey : GMUserPasskey, IAggregateRoot
{
    private UserPasskey() { } // EF Core materialization

    private UserPasskey(Guid userId, string credentialId, byte[] publicKeySpki, string name)
        : base(userId, credentialId, publicKeySpki, name) { }

    public static UserPasskey Create(Guid userId, string credentialId, byte[] publicKeySpki, string name) =>
        new(userId, credentialId, publicKeySpki, name);
}
