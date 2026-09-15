using GM.Exceptions;
using GM.Identity.Sample.Infrastructure.Options;
using Microsoft.IdentityModel.Tokens;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GM.Identity.Sample.Infrastructure.Services.OAuth;

/// <summary>
/// Builds the client secret that Sign in with Apple requires: a short-lived ES256 JWT signed with the
/// developer's .p8 private key (rather than a static string), with <c>iss</c>=Team ID, <c>sub</c>=client id,
/// <c>aud</c>=<c>https://appleid.apple.com</c>, and the key id in the header. Regenerated per exchange, so it
/// never outlives its use.
/// </summary>
public static class AppleClientSecretGenerator
{
    private const string AppleAudience = "https://appleid.apple.com";

    /// <summary>
    /// Creates the signed client secret. <paramref name="privateKeyPem"/> is the PEM contents of the Apple .p8
    /// EC private key (resolved from the secrets store, never bound from config).
    /// </summary>
    public static string Generate(OAuthProviderOptions provider, string privateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(provider.TeamId) || string.IsNullOrWhiteSpace(provider.KeyId))
            throw new CustomException("Apple sign-in requires TeamId and KeyId to be configured.");

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);

        var securityKey = new ECDsaSecurityKey(ecdsa) { KeyId = provider.KeyId };
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256);

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: provider.TeamId,
            audience: AppleAudience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, provider.ClientId)],
            notBefore: now,
            // Apple allows up to 6 months; keep it short since it is minted per request.
            expires: now.AddMinutes(5),
            signingCredentials: credentials);
        token.Payload[JwtRegisteredClaimNames.Iat] = new DateTimeOffset(now).ToUnixTimeSeconds();

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
