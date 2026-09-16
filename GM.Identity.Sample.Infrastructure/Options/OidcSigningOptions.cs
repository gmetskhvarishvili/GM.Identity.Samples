using System.Collections.Generic;

namespace GM.Identity.Sample.Infrastructure.Options;

/// <summary>
/// The OP's token-signing keys, bound from the <c>Oidc</c> configuration section. The active key signs new
/// tokens; previous keys are published in the JWKS and accepted for verification so a key can be rotated
/// without invalidating tokens (or logout/id_token hints) still in flight. Configure the active key in
/// production so it survives restarts and is shared across instances; a dev key is generated when it is absent.
/// </summary>
public sealed class OidcSigningOptions
{
    public const string SectionName = "Oidc";

    /// <summary>PEM of the active EC (P-256) private key that signs new tokens. Generated per-process if unset.</summary>
    public string? SigningKeyPem { get; set; }

    /// <summary>
    /// PEMs of superseded keys kept for verification during a rotation overlap (public or private PEM — only
    /// the public part is used). Published in the JWKS alongside the active key; not used to sign.
    /// </summary>
    public List<string> PreviousSigningKeyPems { get; set; } = new();
}
