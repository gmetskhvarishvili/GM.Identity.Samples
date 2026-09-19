using System.Collections.Generic;
using System.Text.Json.Serialization;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Clients;

/// <summary>RFC 7591 dynamic client registration request body (snake_case on the wire).</summary>
public class RegisterClientModel
{
    [Display(ResourceType = typeof(StringResource), Name = "ClientName")]
    [JsonPropertyName("client_name")] public string ClientName { get; set; } = null!;
    [Display(ResourceType = typeof(StringResource), Name = "RedirectUris")]
    [JsonPropertyName("redirect_uris")] public List<string> RedirectUris { get; set; } = new();
    [Display(ResourceType = typeof(StringResource), Name = "Scope")]
    [JsonPropertyName("scope")] public string? Scope { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "BackchannelLogoutUri")]
    [JsonPropertyName("backchannel_logout_uri")] public string? BackchannelLogoutUri { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "FrontchannelLogoutUri")]
    [JsonPropertyName("frontchannel_logout_uri")] public string? FrontchannelLogoutUri { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "RequireConsent")]
    [JsonPropertyName("require_consent")] public bool RequireConsent { get; set; }
}
