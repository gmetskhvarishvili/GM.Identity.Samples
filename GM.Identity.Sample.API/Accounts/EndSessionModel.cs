using Microsoft.AspNetCore.Mvc;

using System;

namespace GM.Identity.Sample.API.Accounts;

/// <summary>Form body for the OpenID Connect end-session (logout) endpoint. The SSO session itself is taken
/// from the browser's SSO cookie, not the body.</summary>
public class EndSessionModel
{
    [FromForm(Name = "client_id")]
    public Guid? ClientId { get; set; }

    [FromForm(Name = "post_logout_redirect_uri")]
    public string? PostLogoutRedirectUri { get; set; }

    [FromForm(Name = "state")]
    public string? State { get; set; }

    [FromForm(Name = "id_token_hint")]
    public string? IdTokenHint { get; set; }
}
