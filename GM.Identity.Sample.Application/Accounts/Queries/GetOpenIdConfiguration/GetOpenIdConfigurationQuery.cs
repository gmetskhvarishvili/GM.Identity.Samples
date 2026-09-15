using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Queries.GetOpenIdConfiguration;

/// <summary>
/// Builds the OpenID Connect discovery document for this provider. The <paramref name="Issuer"/> (the request's
/// scheme+host) anchors every advertised endpoint. Tokens are opaque, so no signing/JWKS metadata is advertised;
/// resource servers validate tokens via the introspection endpoint instead.
/// </summary>
public record GetOpenIdConfigurationQuery(string Issuer) : IRequest<OpenIdConfigurationDto>;

public class GetOpenIdConfigurationQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetOpenIdConfigurationQuery, OpenIdConfigurationDto>
{
    public async Task<OpenIdConfigurationDto> Handle(GetOpenIdConfigurationQuery request, CancellationToken cancellationToken)
    {
        var issuer = request.Issuer.TrimEnd('/');

        var dbScopes = await unitOfWork.ScopeRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        var scopes = new List<string> { "openid" };
        scopes.AddRange(dbScopes.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct());

        return new OpenIdConfigurationDto
        {
            Issuer = issuer,
            AuthorizationEndpoint = $"{issuer}/connect/authorize",
            TokenEndpoint = $"{issuer}/connect/token",
            IntrospectionEndpoint = $"{issuer}/connect/introspect",
            RevocationEndpoint = $"{issuer}/connect/revoke",
            EndSessionEndpoint = $"{issuer}/connect/endsession",
            UserInfoEndpoint = $"{issuer}/connect/userinfo",
            JwksUri = $"{issuer}/.well-known/jwks.json",
            BackchannelLogoutSupported = true,
            BackchannelLogoutSessionSupported = true,
            ScopesSupported = scopes,
            ResponseTypesSupported = new[] { "code" },
            GrantTypesSupported = new[] { "authorization_code", "refresh_token", "password", "ClientCredentials" },
            CodeChallengeMethodsSupported = new[] { "S256" },
            TokenEndpointAuthMethodsSupported = new[] { "client_secret_post" },
            SubjectTypesSupported = new[] { "public" },
        };
    }
}

public class OpenIdConfigurationDto
{
    [JsonPropertyName("issuer")] public string Issuer { get; set; } = null!;
    [JsonPropertyName("authorization_endpoint")] public string AuthorizationEndpoint { get; set; } = null!;
    [JsonPropertyName("token_endpoint")] public string TokenEndpoint { get; set; } = null!;
    [JsonPropertyName("introspection_endpoint")] public string IntrospectionEndpoint { get; set; } = null!;
    [JsonPropertyName("revocation_endpoint")] public string RevocationEndpoint { get; set; } = null!;
    [JsonPropertyName("end_session_endpoint")] public string EndSessionEndpoint { get; set; } = null!;
    [JsonPropertyName("userinfo_endpoint")] public string UserInfoEndpoint { get; set; } = null!;
    [JsonPropertyName("jwks_uri")] public string JwksUri { get; set; } = null!;
    [JsonPropertyName("backchannel_logout_supported")] public bool BackchannelLogoutSupported { get; set; }
    [JsonPropertyName("backchannel_logout_session_supported")] public bool BackchannelLogoutSessionSupported { get; set; }
    [JsonPropertyName("scopes_supported")] public IReadOnlyCollection<string> ScopesSupported { get; set; } = new List<string>();
    [JsonPropertyName("response_types_supported")] public IReadOnlyCollection<string> ResponseTypesSupported { get; set; } = new List<string>();
    [JsonPropertyName("grant_types_supported")] public IReadOnlyCollection<string> GrantTypesSupported { get; set; } = new List<string>();
    [JsonPropertyName("code_challenge_methods_supported")] public IReadOnlyCollection<string> CodeChallengeMethodsSupported { get; set; } = new List<string>();
    [JsonPropertyName("token_endpoint_auth_methods_supported")] public IReadOnlyCollection<string> TokenEndpointAuthMethodsSupported { get; set; } = new List<string>();
    [JsonPropertyName("subject_types_supported")] public IReadOnlyCollection<string> SubjectTypesSupported { get; set; } = new List<string>();
}
