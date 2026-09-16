using GM.Identity.Sample.Application.Infrastructure.Services.Logout;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using System.Collections.Generic;

namespace GM.Identity.Sample.Infrastructure.Services.Logout;

/// <summary>
/// Builds the OpenID Connect id_token — an ES256 JWT signed with the OP key (verifiable via
/// <c>/.well-known/jwks.json</c>) — carrying the standard identity claims. <c>sid</c> ties the token to the SSO
/// session so it correlates with back-/front-channel logout; <c>nonce</c> is echoed when the client supplied one.
/// </summary>
public sealed class IdTokenGenerator(OidcSigningKey signingKey) : IIdTokenGenerator
{
    public string Generate(IdTokenParameters parameters)
    {
        var p = parameters;
        var claims = new Dictionary<string, object>
        {
            ["sub"] = p.UserId.ToString(),
            ["sid"] = p.SessionId.ToString(),
            ["auth_time"] = new System.DateTimeOffset(p.AuthTime).ToUnixTimeSeconds(),
        };

        if (!string.IsNullOrWhiteSpace(p.Email))
        {
            claims["email"] = p.Email;
            claims["email_verified"] = p.EmailVerified;
        }
        if (!string.IsNullOrWhiteSpace(p.Name))
            claims["name"] = p.Name;
        if (!string.IsNullOrWhiteSpace(p.Nonce))
            claims["nonce"] = p.Nonce;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = p.Issuer,
            Audience = p.ClientId.ToString(),
            IssuedAt = System.DateTime.UtcNow,
            NotBefore = System.DateTime.UtcNow,
            Expires = p.ExpiresAt,
            SigningCredentials = signingKey.SigningCredentials,
            Claims = claims,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
