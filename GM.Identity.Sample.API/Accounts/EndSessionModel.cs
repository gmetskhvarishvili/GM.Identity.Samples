using Microsoft.AspNetCore.Mvc;

using System;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Accounts;

/// <summary>Form body for the OpenID Connect end-session (logout) endpoint. The SSO session itself is taken
/// from the browser's SSO cookie, not the body.</summary>
public class EndSessionModel
{
    /// <summary>The client id.</summary>
    [FromForm(Name = "client_id")]
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid? ClientId { get; set; }

    /// <summary>The post logout redirect uri.</summary>
    [FromForm(Name = "post_logout_redirect_uri")]
    [Display(ResourceType = typeof(StringResource), Name = "PostLogoutRedirectUri")]
    public string? PostLogoutRedirectUri { get; set; }

    /// <summary>The state.</summary>
    [FromForm(Name = "state")]
    [Display(ResourceType = typeof(StringResource), Name = "State")]
    public string? State { get; set; }

    /// <summary>The id token hint.</summary>
    [FromForm(Name = "id_token_hint")]
    [Display(ResourceType = typeof(StringResource), Name = "IdTokenHint")]
    public string? IdTokenHint { get; set; }
}
