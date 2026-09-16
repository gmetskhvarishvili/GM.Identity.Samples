using Microsoft.AspNetCore.Mvc;

using System;

namespace GM.Identity.Sample.API.Accounts;

/// <summary>Form body for the authorization endpoint of the PKCE authorization-code flow.</summary>
public class AuthorizeCodeModel
{
    [FromForm(Name = "client_id")]
    public Guid ClientId { get; set; }

    [FromForm(Name = "redirect_uri")]
    public string RedirectUri { get; set; } = null!;

    [FromForm(Name = "scope")]
    public string? Scope { get; set; }

    [FromForm(Name = "state")]
    public string? State { get; set; }

    [FromForm(Name = "code_challenge")]
    public string CodeChallenge { get; set; } = null!;

    [FromForm(Name = "code_challenge_method")]
    public string CodeChallengeMethod { get; set; } = "S256";

    [FromForm(Name = "username")]
    public string? UserName { get; set; }

    [FromForm(Name = "password")]
    public string? Password { get; set; }

    /// <summary>OIDC prompt: <c>none</c> (silent-only) or <c>login</c> (force re-authentication).</summary>
    [FromForm(Name = "prompt")]
    public string? Prompt { get; set; }

    /// <summary>OIDC nonce, echoed into the id_token to bind it to this authorization request.</summary>
    [FromForm(Name = "nonce")]
    public string? Nonce { get; set; }
}
