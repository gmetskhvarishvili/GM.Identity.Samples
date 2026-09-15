using GM.Identity.Sample.Application.Infrastructure.Services.Logout;
using Microsoft.IdentityModel.Tokens;

using System.Collections.Generic;
using System.Security.Cryptography;

namespace GM.Identity.Sample.Infrastructure.Services.Logout;

/// <summary>
/// The OP's asymmetric signing key (ECDSA P-256 / ES256) used to sign back-channel logout tokens, published as a
/// JWK Set so relying parties can verify them. Loaded from <c>Oidc:SigningKeyPem</c> when configured (persist it
/// in production so the key survives restarts), otherwise a key is generated for this process — fine for a
/// sample, since relying parties fetch the current public key from the JWKS endpoint at verification time.
/// Registered as a singleton so the key is stable for the process lifetime.
/// </summary>
public sealed class OidcSigningKey : IJwksProvider
{
    private readonly ECDsa _ecdsa;

    public OidcSigningKey(string? pem)
    {
        _ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        if (!string.IsNullOrWhiteSpace(pem))
            _ecdsa.ImportFromPem(pem);

        var parameters = _ecdsa.ExportParameters(includePrivateParameters: false);
        // A stable key id derived from the public coordinates, so it does not change unless the key does.
        byte[] publicPoint = [.. parameters.Q.X!, .. parameters.Q.Y!];
        KeyId = Base64UrlEncoder.Encode(SHA256.HashData(publicPoint))[..16];

        var securityKey = new ECDsaSecurityKey(_ecdsa) { KeyId = KeyId };
        SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256);
    }

    /// <summary>The key id stamped in signed tokens' headers and published in the JWK Set.</summary>
    public string KeyId { get; }

    /// <summary>Credentials for signing tokens with this key (ES256).</summary>
    public SigningCredentials SigningCredentials { get; }

    public JsonWebKeySetDto GetKeys()
    {
        var parameters = _ecdsa.ExportParameters(includePrivateParameters: false);
        return new JsonWebKeySetDto
        {
            Keys = new List<JsonWebKeyDto>
            {
                new()
                {
                    KeyType = "EC",
                    Use = "sig",
                    Algorithm = SecurityAlgorithms.EcdsaSha256, // ES256
                    KeyId = KeyId,
                    Curve = "P-256",
                    X = Base64UrlEncoder.Encode(parameters.Q.X),
                    Y = Base64UrlEncoder.Encode(parameters.Q.Y),
                },
            },
        };
    }
}
