using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GM.Identity.Sample.API.Clients;

/// <summary>RFC 7591 dynamic client registration request body (snake_case on the wire).</summary>
public class RegisterClientModel
{
    [JsonPropertyName("client_name")] public string ClientName { get; set; } = null!;
    [JsonPropertyName("redirect_uris")] public List<string> RedirectUris { get; set; } = new();
    [JsonPropertyName("scope")] public string? Scope { get; set; }
    [JsonPropertyName("backchannel_logout_uri")] public string? BackchannelLogoutUri { get; set; }
    [JsonPropertyName("frontchannel_logout_uri")] public string? FrontchannelLogoutUri { get; set; }
    [JsonPropertyName("require_consent")] public bool RequireConsent { get; set; }
}
