using GM.EntityFramework.Domain.Abstractions;
using GM.EntityFramework.Domain.Base;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate;

/// <summary>
/// A registered WebAuthn/FIDO2 passkey for a user: the credential id and its ES256 public key (stored as a
/// SubjectPublicKeyInfo blob). The private key never leaves the authenticator; login is proven by an assertion
/// signature the server verifies against this public key.
/// </summary>
public class UserPasskey : SoftDeletableEntity<Guid>, IAggregateRoot
{
    private UserPasskey() { } // EF Core materialization

    private UserPasskey(Guid userId, string credentialId, byte[] publicKeySpki, string name)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CredentialId = credentialId;
        PublicKeySpki = publicKeySpki;
        Name = name;
    }

    public static UserPasskey Create(Guid userId, string credentialId, byte[] publicKeySpki, string name) =>
        new(userId, credentialId, publicKeySpki, name);

    public Guid UserId { get; private set; }

    /// <summary>The credential id (base64url), unique per authenticator.</summary>
    public string CredentialId { get; private set; } = null!;

    /// <summary>The ES256 public key as a SubjectPublicKeyInfo (DER) blob.</summary>
    public byte[] PublicKeySpki { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Signature counter last seen from the authenticator (clone-detection aid).</summary>
    public long SignCount { get; private set; }

    public void RecordUse(long signCount)
    {
        if (signCount > SignCount) SignCount = signCount;
    }
}
