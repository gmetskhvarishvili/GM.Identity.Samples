namespace GM.Identity.Sample.Infrastructure.Options;

public class OAuthProviderOptions
{
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public string AuthorizationEndpoint { get; set; } = default!;
    public string TokenEndpoint { get; set; } = default!;
    public string Scope { get; set; } = default!;
    public string TokenName { get; set; } = default!;
    public string? UserDetailsEndpoint { get; set; }
}

public class OAuthOptions : Dictionary<string, OAuthProviderOptions>;