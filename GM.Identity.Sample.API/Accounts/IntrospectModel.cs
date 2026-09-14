using Microsoft.AspNetCore.Mvc;

using System;

namespace GM.Identity.Sample.API.Accounts;

/// <summary>Form body for the RFC 7662 token-introspection endpoint.</summary>
public class IntrospectModel
{
    [FromForm(Name = "token")]
    public string Token { get; set; } = null!;

    [FromForm(Name = "token_type_hint")]
    public string? TokenTypeHint { get; set; }

    [FromForm(Name = "client_id")]
    public Guid ClientId { get; set; }

    [FromForm(Name = "client_secret")]
    public string ClientSecret { get; set; } = null!;
}
