using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using GM.Exceptions;
using GM.Identity;
using GM.Identity.Sample.Application.Infrastructure.Services.OAuth;
using GM.Identity.Sample.Infrastructure.Options;
using GM.Secrets;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Services.OAuth;

public class OAuthService(
    IOptions<OAuthOptions> options,
    IHttpClientFactory httpClientFactory,
    IConnectionMultiplexer redis,
    ISecretsService secrets) : IOAuthService
{
    // The PKCE verifier for an external-login round trip is held in Redis, keyed by the opaque state value, so
    // the state survives across the two requests (redirect, then callback) regardless of which instance handles
    // each — distributed and thread-safe, unlike a process-local dictionary — and self-evicts if the user never
    // returns. It is single-use: the callback consumes it atomically (GETDEL).
    private const string StateKeyPrefix = "gm-identity:oauth:pkce:";
    private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(10);

    private readonly OAuthOptions _providers = options.Value;

    public async Task<string> GetRedirectUri(GetRedirectUriDto request, CancellationToken cancellationToken)
    {
        var codeVerifier = PkceHelper.GenerateCodeVerifier();
        var codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);
        var state = Guid.NewGuid().ToString("N");

        // Bind the verifier to this state for the callback to reclaim (single-use, auto-expiring).
        await redis.GetDatabase().StringSetAsync(StateKeyPrefix + state, codeVerifier, StateTtl);

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
        // Atomically fetch-and-delete the verifier bound to this state: a missing value means an unknown,
        // already-consumed, or expired state (replay/forgery) — reject it.
        var codeVerifier = await redis.GetDatabase().StringGetDeleteAsync(StateKeyPrefix + request.State);
        if (codeVerifier.IsNullOrEmpty)
            throw new CustomException("Missing or invalid state");

        // Resolve the provider's client secret at exchange time through the secrets abstraction rather
        // than reading it from bound options, so the backing store (config today, a vault tomorrow) is
        // swappable without changing this call site.
        var clientSecret = await secrets.GetRequiredSecretAsync(
            $"OAuth:{request.Provider}:ClientSecret", cancellationToken);

        var client = httpClientFactory.CreateClient();
        var tokenResp = await client.PostAsync(_providers[request.Provider].TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _providers[request.Provider].ClientId,
                ["client_secret"] = clientSecret,
                ["code"] = request.Code,
                ["code_verifier"] = codeVerifier!,
                ["redirect_uri"] = request.RedirectUri,
                ["grant_type"] = "authorization_code"
            }), cancellationToken);

        if (!tokenResp.IsSuccessStatusCode)
            throw new CustomException($"Failed to exchange code with Google, statusCode: {(int)tokenResp.StatusCode}");

        var tokenJson =
            await tokenResp.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: cancellationToken);
        var token = tokenJson![_providers[request.Provider].TokenName].ToString() ?? throw new CustomException("id_token missing");

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
