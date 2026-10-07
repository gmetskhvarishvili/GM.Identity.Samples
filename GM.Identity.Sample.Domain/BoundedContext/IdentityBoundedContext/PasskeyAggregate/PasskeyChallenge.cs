using GM.EntityFramework.Domain.Abstractions;
using GM.Identity.Domain.Identity.PasskeyChallengeAggregate.Entities;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate;

/// <summary>
/// The sample's concrete passkey-login challenge aggregate root. Its shape lives in the GM.Identity base
/// <see cref="GMPasskeyChallenge"/>; this type fixes it as the application's aggregate and exposes the factory,
/// mirroring how the other sample entities specialise their GM.Identity bases.
/// </summary>
public class PasskeyChallenge : GMPasskeyChallenge, IAggregateRoot
{
    private PasskeyChallenge() { } // EF Core materialization

    private PasskeyChallenge(Guid userId, string challenge, DateTime expiresAt)
        : base(userId, challenge, expiresAt) { }

    public static PasskeyChallenge Create(Guid userId, string challenge, DateTime expiresAt) =>
        new(userId, challenge, expiresAt);
}
