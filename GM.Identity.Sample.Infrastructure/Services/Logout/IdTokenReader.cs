using GM.Identity.Sample.Application.Infrastructure.Services.Logout;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Services.Logout;

/// <summary>
/// <see cref="IIdTokenReader"/> that validates an id_token against the OP's own signing key (signature only —
/// lifetime, audience and issuer are not enforced, since a logout hint is by design presented after expiry and
/// from any of the OP's clients) and extracts its <c>sid</c>.
/// </summary>
public sealed class IdTokenReader(OidcSigningKey signingKey) : IIdTokenReader
{
    public async Task<Guid?> TryReadSessionIdAsync(string? idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            return null;

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            IssuerSigningKey = signingKey.SigningCredentials.Key,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,
            ValidateAudience = false,
            ValidateIssuer = false,
        });

        if (!result.IsValid || result.SecurityToken is not JsonWebToken jwt)
            return null;

        return jwt.TryGetClaim("sid", out var sid) && Guid.TryParse(sid.Value, out var sessionId)
            ? sessionId
            : null;
    }
}
