using Microsoft.AspNetCore.Mvc;

using System;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Accounts;

/// <summary>Form body for the authorization endpoint of the PKCE authorization-code flow.</summary>
public class AuthorizeCodeModel
{
    /// <summary>The client id.</summary>
    [FromForm(Name = "client_id")]
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid ClientId { get; set; }

    /// <summary>The redirect uri.</summary>
    [FromForm(Name = "redirect_uri")]
    [Display(ResourceType = typeof(StringResource), Name = "RedirectUri")]
    public string RedirectUri { get; set; } = null!;

    /// <summary>The scope.</summary>
    [FromForm(Name = "scope")]
    [Display(ResourceType = typeof(StringResource), Name = "Scope")]
    public string? Scope { get; set; }

    /// <summary>The state.</summary>
    [FromForm(Name = "state")]
    [Display(ResourceType = typeof(StringResource), Name = "State")]
    public string? State { get; set; }

    /// <summary>The code challenge.</summary>
    [FromForm(Name = "code_challenge")]
    [Display(ResourceType = typeof(StringResource), Name = "CodeChallenge")]
    public string CodeChallenge { get; set; } = null!;

    /// <summary>The code challenge method.</summary>
    [FromForm(Name = "code_challenge_method")]
    [Display(ResourceType = typeof(StringResource), Name = "CodeChallengeMethod")]
    public string CodeChallengeMethod { get; set; } = "S256";

    /// <summary>The user name.</summary>
    [FromForm(Name = "username")]
    [Display(ResourceType = typeof(StringResource), Name = "UserName")]
    public string? UserName { get; set; }

    /// <summary>The password.</summary>
    [FromForm(Name = "password")]
    [Display(ResourceType = typeof(StringResource), Name = "Password")]
    public string? Password { get; set; }

    /// <summary>OIDC prompt: <c>none</c> (silent-only) or <c>login</c> (force re-authentication).</summary>
    [FromForm(Name = "prompt")]
    [Display(ResourceType = typeof(StringResource), Name = "Prompt")]
    public string? Prompt { get; set; }

    /// <summary>OIDC nonce, echoed into the id_token to bind it to this authorization request.</summary>
    [FromForm(Name = "nonce")]
    [Display(ResourceType = typeof(StringResource), Name = "Nonce")]
    public string? Nonce { get; set; }

    /// <summary>The user's approval of the requested scopes (for consent-requiring clients).</summary>
    [FromForm(Name = "consent")]
    [Display(ResourceType = typeof(StringResource), Name = "Consent")]
    public bool Consent { get; set; }
}
