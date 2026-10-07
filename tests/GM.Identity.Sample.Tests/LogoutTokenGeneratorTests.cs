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
/// Unit tests for the back-channel logout token: it must be an ES256 JWT verifiable with the OP's published
/// JWKS, carrying the subject and session that ended plus the spec-required <c>events</c> member, and it must
/// NOT contain a nonce (so it can never be mistaken for an id_token). Pure crypto — no infrastructure.
/// </summary>
public sealed class LogoutTokenGeneratorTests
{
    [Fact]
    public async Task Generates_an_es256_logout_token_verifiable_against_the_published_jwks()
    {
        var signingKey = new OidcSigningKey(new OidcSigningOptions());
        var generator = new LogoutTokenGenerator(signingKey);

        const string issuer = "https://op.example";
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var token = generator.Generate(issuer, clientId, userId, sessionId);

        // Reconstruct the verification key from the PUBLISHED JWK — exactly what a relying party would do.
        var jwk = signingKey.GetKeys().Keys.Single();
        using var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = Base64UrlEncoder.DecodeBytes(jwk.X),
                Y = Base64UrlEncoder.DecodeBytes(jwk.Y),
            },
        });

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = clientId.ToString(),
            IssuerSigningKey = new ECDsaSecurityKey(ecdsa) { KeyId = jwk.KeyId },
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
        });

        Assert.True(result.IsValid);

        var jwt = (JsonWebToken)result.SecurityToken;
        Assert.Equal(userId.ToString(), jwt.Subject);
        Assert.Equal(sessionId.ToString(), jwt.GetClaim("sid").Value);
        Assert.False(string.IsNullOrEmpty(jwt.GetClaim("jti").Value));
        Assert.Contains(LogoutTokenGenerator.BackchannelLogoutEvent, jwt.GetClaim("events").Value);
        Assert.False(jwt.TryGetClaim("nonce", out _)); // forbidden by the spec
    }
}
