using System;
using System.Security.Cryptography;

namespace GM.Identity.Sample.Application.Common.Security;

/// <summary>
/// Verifies a WebAuthn (FIDO2) assertion signature for an ES256 (P-256 / SHA-256) passkey — the security-
/// critical step of a passkey login: the authenticator signs <c>authenticatorData || SHA256(clientDataJSON)</c>
/// with the credential's private key, and the server verifies it with the stored public key.
/// <para>
/// This implements assertion verification (the login path). A production system should additionally verify
/// registration attestation (via a full FIDO2 library), the RP-ID hash, and the user-presence/verification
/// flags; those are out of scope for this sample.
/// </para>
/// </summary>
public static class WebAuthnAssertion
{
    /// <summary>
    /// Returns true if <paramref name="signature"/> (ASN.1 DER ECDSA) is valid for the passkey identified by
    /// <paramref name="publicKeySpki"/> (a SubjectPublicKeyInfo-encoded P-256 key) over the WebAuthn signed data.
    /// </summary>
    public static bool VerifyEs256(
        byte[] publicKeySpki, byte[] authenticatorData, byte[] clientDataJson, byte[] signature)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKeySpki, out _);

            var clientDataHash = SHA256.HashData(clientDataJson);
            var signedData = new byte[authenticatorData.Length + clientDataHash.Length];
            Buffer.BlockCopy(authenticatorData, 0, signedData, 0, authenticatorData.Length);
            Buffer.BlockCopy(clientDataHash, 0, signedData, authenticatorData.Length, clientDataHash.Length);

            return ecdsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Decodes a base64url string (no padding), as used by WebAuthn for binary fields.</summary>
    public static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }

    /// <summary>Encodes bytes as base64url (no padding).</summary>
    public static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
