using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using System;
using System.Collections.Generic;

namespace GM.Identity.Sample.Infrastructure.Services.Logout;

/// <summary>
/// Builds the OpenID Connect back-channel logout token: a short-lived JWT, signed with the OP key, whose claims
/// (per the Back-Channel Logout 1.0 spec) identify the subject and session that ended and carry the required
/// <c>events</c> member. It deliberately omits <c>nonce</c> (forbidden), so an RP cannot mistake it for an
/// id_token.
/// </summary>
public sealed class LogoutTokenGenerator(OidcSigningKey signingKey)
{
    /// <summary>The event URI that marks a token as a back-channel logout token.</summary>
    public const string BackchannelLogoutEvent = "http://schemas.openid.net/event/backchannel-logout";

    public string Generate(string issuer, Guid clientId, Guid userId, Guid sessionId)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = clientId.ToString(),
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(2), // logout tokens are consumed immediately
            SigningCredentials = signingKey.SigningCredentials,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = userId.ToString(),
                ["sid"] = sessionId.ToString(),
                ["jti"] = Guid.NewGuid().ToString("N"),
                // Required by the spec; the nested empty object signals a back-channel logout event.
                ["events"] = new Dictionary<string, object> { [BackchannelLogoutEvent] = new Dictionary<string, object>() },
            },
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
