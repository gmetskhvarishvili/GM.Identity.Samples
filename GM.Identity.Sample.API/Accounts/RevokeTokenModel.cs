using Microsoft.AspNetCore.Mvc;

using System;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Accounts;

/// <summary>Revoke token model.</summary>
public class RevokeTokenModel
{
    /// <summary>The token.</summary>
    [FromForm(Name = "token")]
    [Display(ResourceType = typeof(StringResource), Name = "Token")]
    public string Token { get; set; } = null!;

    /// <summary>The client id.</summary>
    [FromForm(Name = "client_id")]
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid ClientId { get; set; }

    /// <summary>The client secret.</summary>
    [FromForm(Name = "client_secret")]
    [Display(ResourceType = typeof(StringResource), Name = "ClientSecret")]
    public string ClientSecret { get; set; } = null!;
}
