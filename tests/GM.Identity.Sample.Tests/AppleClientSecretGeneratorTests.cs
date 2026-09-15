using GM.Exceptions;
using GM.Identity.Sample.Infrastructure.Options;
using GM.Identity.Sample.Infrastructure.Services.OAuth;
using Xunit;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// Unit tests for Sign in with Apple's client-secret JWT: it must be an ES256 token signed with the .p8 EC key,
/// carrying iss=Team ID, sub=client id, aud=appleid.apple.com and the key id in the header. Pure crypto — no
/// infrastructure required.
/// </summary>
public sealed class AppleClientSecretGeneratorTests
{
    private const string TeamId = "TEAM123456";
    private const string KeyId = "KEY1234567";
    private const string ClientId = "com.example.app";

    [Fact]
    public void Generate_produces_a_signed_es256_jwt_with_the_expected_claims()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var provider = new OAuthProviderOptions
        {
            ClientId = ClientId,
            TeamId = TeamId,
            KeyId = KeyId,
            SecretKind = SecretKind.AppleJwt,
        };

        var secret = AppleClientSecretGenerator.Generate(provider, ecdsa.ExportPkcs8PrivateKeyPem());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(secret);
        Assert.Equal(TeamId, jwt.Issuer);
        Assert.Contains("https://appleid.apple.com", jwt.Audiences);
        Assert.Equal(ClientId, jwt.Subject);
        Assert.Equal(KeyId, jwt.Header.Kid);
        Assert.Equal("ES256", jwt.SignatureAlgorithm);
    }

    [Fact]
    public void Generate_requires_team_id_and_key_id()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pem = ecdsa.ExportPkcs8PrivateKeyPem();
        var provider = new OAuthProviderOptions { ClientId = ClientId, SecretKind = SecretKind.AppleJwt };

        Assert.Throws<CustomException>(() => AppleClientSecretGenerator.Generate(provider, pem));
    }
}
