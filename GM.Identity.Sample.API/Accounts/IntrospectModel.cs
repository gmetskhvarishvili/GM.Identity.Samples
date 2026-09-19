using Microsoft.AspNetCore.Mvc;

using System;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Accounts;

/// <summary>Form body for the RFC 7662 token-introspection endpoint.</summary>
public class IntrospectModel
{
    [FromForm(Name = "token")]
    [Display(ResourceType = typeof(StringResource), Name = "Token")]
    public string Token { get; set; } = null!;

    [FromForm(Name = "token_type_hint")]
    [Display(ResourceType = typeof(StringResource), Name = "TokenTypeHint")]
    public string? TokenTypeHint { get; set; }

    [FromForm(Name = "client_id")]
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid ClientId { get; set; }

    [FromForm(Name = "client_secret")]
    [Display(ResourceType = typeof(StringResource), Name = "ClientSecret")]
    public string ClientSecret { get; set; } = null!;
}
