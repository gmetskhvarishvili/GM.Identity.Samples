using System;

namespace GM.Identity.Sample.Application.Infrastructure.Services.Logout;

/// <summary>The identity claims to mint into an OpenID Connect id_token.</summary>
public sealed record IdTokenParameters(
    string Issuer,
    Guid ClientId,
    Guid UserId,
    Guid SessionId,
    DateTime AuthTime,
    DateTime ExpiresAt,
    string? Email,
    bool EmailVerified,
    string? Name,
    string? Nonce);

/// <summary>
/// Issues signed OpenID Connect id_tokens (ES256, verifiable via the OP's JWKS). Complements the opaque access
/// tokens: relying parties get a self-contained, offline-verifiable assertion of who signed in.
/// </summary>
public interface IIdTokenGenerator
{
    string Generate(IdTokenParameters parameters);
}
