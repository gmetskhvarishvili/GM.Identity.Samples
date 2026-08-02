using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common.Security;
using GM.Identity.Sample.Application.Infrastructure.Services.OAuth;
using GM.Identity.Sample.Infrastructure.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace GM.Identity.Sample.Infrastructure.Services.OAuth;

public class OAuthService(IOptions<OAuthOptions> options, IHttpClientFactory httpClientFactory) : IOAuthService
{
    private static readonly Dictionary<string, string> VerifierStore = new();
    private readonly OAuthOptions _providers = options.Value;
    
    public string GetRedirectUri(GetRedirectUriDto request)
    {
        var codeVerifier = PkceHelper.GenerateCodeVerifier();
        var codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);
        var state = Guid.NewGuid().ToString();

        // Save verifier for this state
        VerifierStore[state] = codeVerifier;

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _providers[request.Provider].ClientId,
            ["redirect_uri"] = request.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = _providers[request.Provider].Scope,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["access_type"] = "offline",
            ["prompt"] = "consent"
        };

        return QueryHelpers.AddQueryString(_providers[request.Provider].AuthorizationEndpoint, query);
    }
    
    public async Task<string> GetEmail(GetEmailDto request, CancellationToken cancellationToken)
    {
        if (!VerifierStore.Remove(request.State, out var codeVerifier))
           throw new CustomException("Missing or invalid state");

        var client = httpClientFactory.CreateClient();
        var tokenResp = await client.PostAsync(_providers[request.Provider].TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _providers[request.Provider].ClientId,
                ["client_secret"] = _providers[request.Provider].ClientSecret,
                ["code"] = request.Code,
                ["code_verifier"] = codeVerifier,
                ["redirect_uri"] = request.RedirectUri,
                ["grant_type"] = "authorization_code"
            }), cancellationToken);

        if (!tokenResp.IsSuccessStatusCode)
            throw new CustomException($"Failed to exchange code with Google, statusCode: {(int)tokenResp.StatusCode}");

        var tokenJson =
            await tokenResp.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: cancellationToken);
        var token = tokenJson![_providers[request.Provider].TokenName].ToString() ?? throw new Exception("id_token missing");
            
        if (string.IsNullOrEmpty(token))
            throw new CustomException("Missing access token");

        string? email;
        switch (request.Provider)
        {
            case "Google":
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                break;
            }
            case "Facebook":
            {
                // Fetch user info from Facebook Graph API
                var userResp =
                    await client.GetAsync($"{_providers[request.Provider].UserDetailsEndpoint}?fields=id,name,email&access_token={token}",
                        cancellationToken);
                if (!userResp.IsSuccessStatusCode)
                    throw new CustomException($"Failed to fetch user info from Facebook, statusCode: {(int)userResp.StatusCode}");

                var userJson =
                    await userResp.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: cancellationToken);
                email = userJson!.GetValueOrDefault("email")!.ToString();
                break;
            }
            default:
                throw new CustomException("Invalid Provider");
        }
        return email!;
       
    }
}