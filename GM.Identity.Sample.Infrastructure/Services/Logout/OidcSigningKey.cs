using GM.Identity.Sample.Application.Infrastructure.Services.Logout;
using GM.Identity.Sample.Infrastructure.Options;
using Microsoft.IdentityModel.Tokens;

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace GM.Identity.Sample.Infrastructure.Services.Logout;

/// <summary>
/// The OP's asymmetric token-signing keys (ECDSA P-256 / ES256). New tokens are signed with the <b>active</b>
/// key; the active key plus any configured <b>previous</b> keys are published as a JWK Set and accepted for
/// verification, so a key can be rotated without invalidating tokens (or id_token / logout hints) still in
/// flight. The active key is loaded from <c>Oidc:SigningKeyPem</c> when set (persist it in production so it
/// survives restarts and is shared across instances) and generated per-process otherwise. Registered as a
/// singleton so the keys are stable for the process lifetime.
/// </summary>
public sealed class OidcSigningKey : IJwksProvider
{
    private sealed record KeyEntry(ECDsa Ecdsa, string Kid, ECDsaSecurityKey SecurityKey);

    private readonly IReadOnlyList<KeyEntry> _all; // active first, then previous (verification-only)

    public OidcSigningKey(OidcSigningOptions options)
    {
        var active = BuildEntry(options.SigningKeyPem);
        var entries = new List<KeyEntry> { active };
        foreach (var pem in options.PreviousSigningKeyPems ?? new List<string>())
            if (!string.IsNullOrWhiteSpace(pem))
                entries.Add(BuildEntry(pem));
        _all = entries;

        KeyId = active.Kid;
        SigningCredentials = new SigningCredentials(active.SecurityKey, SecurityAlgorithms.EcdsaSha256);
        ValidationKeys = _all.Select(e => (SecurityKey)e.SecurityKey).ToList();
    }

    /// <summary>Key id of the active (signing) key — stamped in token headers.</summary>
    public string KeyId { get; }

    /// <summary>Credentials for signing new tokens (ES256, active key).</summary>
    public SigningCredentials SigningCredentials { get; }

    /// <summary>Every key accepted for verification (active + previous) — for token validation across rotation.</summary>
    public IReadOnlyCollection<SecurityKey> ValidationKeys { get; }

    public JsonWebKeySetDto GetKeys() => new()
    {
        Keys = _all.Select(ToJwk).ToList(),
    };

    private static KeyEntry BuildEntry(string? pem)
    {
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        if (!string.IsNullOrWhiteSpace(pem))
            ecdsa.ImportFromPem(pem);

        var parameters = ecdsa.ExportParameters(includePrivateParameters: false);
        // A stable key id derived from the public coordinates, so it does not change unless the key does.
        byte[] publicPoint = [.. parameters.Q.X!, .. parameters.Q.Y!];
        var kid = Base64UrlEncoder.Encode(SHA256.HashData(publicPoint))[..16];

        return new KeyEntry(ecdsa, kid, new ECDsaSecurityKey(ecdsa) { KeyId = kid });
    }

    private static JsonWebKeyDto ToJwk(KeyEntry entry)
    {
        var parameters = entry.Ecdsa.ExportParameters(includePrivateParameters: false);
        return new JsonWebKeyDto
        {
            KeyType = "EC",
            Use = "sig",
            Algorithm = SecurityAlgorithms.EcdsaSha256, // ES256
            KeyId = entry.Kid,
            Curve = "P-256",
            X = Base64UrlEncoder.Encode(parameters.Q.X),
            Y = Base64UrlEncoder.Encode(parameters.Q.Y),
        };
    }
}
