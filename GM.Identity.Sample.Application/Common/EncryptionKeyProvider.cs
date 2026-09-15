using System;
using System.Security.Cryptography;
using System.Text;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Supplies the symmetric key used to encrypt PII columns at rest. In production the key is provided via
/// configuration (base64, 32 bytes for AES-256); absent that, a deterministic development key is derived so the
/// sample still runs. A real deployment must configure a managed key (e.g. from GM.Secrets / a key vault).
/// </summary>
public sealed class EncryptionKeyProvider
{
    public byte[] Key { get; }

    public EncryptionKeyProvider(string? configuredBase64Key)
    {
        if (!string.IsNullOrWhiteSpace(configuredBase64Key))
        {
            Key = Convert.FromBase64String(configuredBase64Key);
            if (Key.Length != 32)
                throw new InvalidOperationException("DataProtection key must be 32 bytes (AES-256) when configured.");
        }
        else
        {
            // Development fallback: a fixed 256-bit key derived from a constant passphrase. NOT for production.
            Key = SHA256.HashData(Encoding.UTF8.GetBytes("gm-identity-sample-dev-pii-key"));
        }
    }
}
