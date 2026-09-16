using GM.Identity.Oidc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// Unit tests for OP signing-key rotation: the JWKS publishes the active key plus configured previous keys, a
/// token signed by a now-previous key still verifies against the rotated key set, and a token from an unrelated
/// key is rejected. Pure crypto — no infrastructure.
/// </summary>
public sealed class OidcSigningKeyRotationTests
{
    [Fact]
    public async Task Tokens_signed_by_a_previous_key_still_verify_after_rotation()
    {
        using var keyA = ECDsa.Create(ECCurve.NamedCurves.nistP256); // new active key
        using var keyB = ECDsa.Create(ECCurve.NamedCurves.nistP256); // rotated-out (previous) key

        // Active = A, with B kept as a previous key for the overlap.
        var rotated = new OidcSigningKey(new OidcSigningOptions
        {
            SigningKeyPem = keyA.ExportPkcs8PrivateKeyPem(),
            PreviousSigningKeyPems = { keyB.ExportPkcs8PrivateKeyPem() },
        });

        // The JWKS publishes both keys, with distinct ids; the active id is A's.
        var jwks = rotated.GetKeys();
        Assert.Equal(2, jwks.Keys.Count);
        Assert.Equal(2, jwks.Keys.Select(k => k.KeyId).Distinct().Count());
        Assert.Contains(jwks.Keys, k => k.KeyId == rotated.KeyId);

        // A token minted while B was active (B now the previous key) still verifies against the rotated set.
        var whenBWasActive = new OidcSigningKey(new OidcSigningOptions { SigningKeyPem = keyB.ExportPkcs8PrivateKeyPem() });
        var tokenFromB = new IdTokenGenerator(whenBWasActive).Generate(Params());

        var ok = await Validate(tokenFromB, rotated.ValidationKeys);
        Assert.True(ok);

        // A token from a key the OP never published is rejected.
        using var stranger = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var strangerKey = new OidcSigningKey(new OidcSigningOptions { SigningKeyPem = stranger.ExportPkcs8PrivateKeyPem() });
        var forged = new IdTokenGenerator(strangerKey).Generate(Params());

        Assert.False(await Validate(forged, rotated.ValidationKeys));
    }

    private static IdTokenParameters Params() => new(
        Issuer: "https://op.example",
        ClientId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        SessionId: Guid.NewGuid(),
        AuthTime: DateTime.UtcNow,
        ExpiresAt: DateTime.UtcNow.AddMinutes(5),
        Email: null,
        EmailVerified: false,
        Name: "test",
        Nonce: null);

    private static async Task<bool> Validate(string token, System.Collections.Generic.IReadOnlyCollection<SecurityKey> keys)
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            IssuerSigningKeys = keys,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
        });
        return result.IsValid;
    }
}
