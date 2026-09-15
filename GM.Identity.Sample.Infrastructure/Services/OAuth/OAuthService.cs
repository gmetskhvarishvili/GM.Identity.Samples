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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Services.OAuth;

/// <summary>
/// Drives external-identity ("social" / enterprise) login for every configured provider through two shapes:
/// OpenID Connect providers (Google, Microsoft, LinkedIn, Apple, any enterprise OIDC IdP) whose token response
/// carries an <c>id_token</c> we read the email claim from, and plain OAuth 2.0 providers (Facebook, GitHub)
/// whose access token we use to call a userinfo endpoint. Adding a compliant OIDC provider is configuration-only.
/// The PKCE verifier is held in Redis (distributed, single-use) between the redirect and the callback.
/// </summary>
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
        var provider = ResolveProvider(request.Provider);

        var codeVerifier = PkceHelper.GenerateCodeVerifier();
        var codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);
        var state = Guid.NewGuid().ToString("N");

        // Bind the verifier to this state for the callback to reclaim (single-use, auto-expiring).
        await redis.GetDatabase().StringSetAsync(StateKeyPrefix + state, codeVerifier, StateTtl);

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = provider.ClientId,
            ["redirect_uri"] = request.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = provider.Scope,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
        };

        // Provider-specific extras (Google's access_type/prompt, Apple's response_mode=form_post, …).
        foreach (var (key, value) in provider.AdditionalAuthorizationParameters)
            query[key] = value;

        return QueryHelpers.AddQueryString(provider.AuthorizationEndpoint, query);
    }

    public async Task<string> GetEmail(GetEmailDto request, CancellationToken cancellationToken)
    {
        var provider = ResolveProvider(request.Provider);

        // Atomically fetch-and-delete the verifier bound to this state: a missing value means an unknown,
        // already-consumed, or expired state (replay/forgery) — reject it.
        var codeVerifier = await redis.GetDatabase().StringGetDeleteAsync(StateKeyPrefix + request.State);
        if (codeVerifier.IsNullOrEmpty)
            throw new CustomException("Missing or invalid state");

        var token = await ExchangeCodeAsync(request, provider, codeVerifier!, cancellationToken);

        var email = provider.Kind switch
        {
            ProviderKind.Oidc => ReadEmailFromIdToken(token, provider),
            ProviderKind.OAuth2 => await ReadEmailFromUserInfoAsync(token, provider, cancellationToken),
            _ => throw new CustomException($"Unsupported provider kind '{provider.Kind}'."),
        };

        if (string.IsNullOrWhiteSpace(email))
            throw new CustomException($"The provider '{request.Provider}' did not return an email address.");

        return email!;
    }

    // Exchanges the authorization code for the provider's token response and returns the configured token
    // (id_token for OIDC, access_token for OAuth2). Sends Accept: application/json (GitHub returns form-encoded
    // otherwise) and a User-Agent (required by the GitHub API).
    private async Task<string> ExchangeCodeAsync(
        GetEmailDto request, OAuthProviderOptions provider, string codeVerifier, CancellationToken cancellationToken)
    {
        var clientSecret = await ResolveClientSecretAsync(request.Provider, provider, cancellationToken);

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GM.Identity.Sample");

        var tokenResp = await client.PostAsync(provider.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = provider.ClientId,
                ["client_secret"] = clientSecret,
                ["code"] = request.Code,
                ["code_verifier"] = codeVerifier,
                ["redirect_uri"] = request.RedirectUri,
                ["grant_type"] = "authorization_code"
            }), cancellationToken);

        if (!tokenResp.IsSuccessStatusCode)
            throw new CustomException(
                $"Failed to exchange code with {request.Provider}, statusCode: {(int)tokenResp.StatusCode}");

        var tokenJson =
            await tokenResp.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: cancellationToken);
        var token = tokenJson is not null && tokenJson.TryGetValue(provider.TokenName, out var value)
            ? value?.ToString()
            : null;

        if (string.IsNullOrEmpty(token))
            throw new CustomException($"The provider '{request.Provider}' response did not contain '{provider.TokenName}'.");

        return token;
    }

    // For Apple the client secret is a per-request ES256 JWT built from the .p8 private key; every other
    // provider uses a static secret. Both are read from the secrets store, never bound from config.
    private async Task<string> ResolveClientSecretAsync(
        string providerName, OAuthProviderOptions provider, CancellationToken cancellationToken)
    {
        if (provider.SecretKind == SecretKind.AppleJwt)
        {
            var privateKeyPem = await secrets.GetRequiredSecretAsync(
                $"OAuth:{providerName}:PrivateKey", cancellationToken);
            return AppleClientSecretGenerator.Generate(provider, privateKeyPem);
        }

        // Resolve the provider's client secret at exchange time through the secrets abstraction rather than
        // reading it from bound options, so the backing store (config today, a vault tomorrow) is swappable.
        return await secrets.GetRequiredSecretAsync($"OAuth:{providerName}:ClientSecret", cancellationToken);
    }

    private static string? ReadEmailFromIdToken(string idToken, OAuthProviderOptions provider)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(idToken);
        return jwt.Claims.FirstOrDefault(c => c.Type == provider.EmailClaim)?.Value;
    }

    // Calls the provider's userinfo endpoint with the access token and extracts the email. Handles both an
    // object response (Facebook: read the configured field) and an array response (GitHub /user/emails: pick
    // the primary verified address).
    private async Task<string?> ReadEmailFromUserInfoAsync(
        string accessToken, OAuthProviderOptions provider, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(provider.UserDetailsEndpoint))
            throw new CustomException("An OAuth2 provider must configure a UserDetailsEndpoint.");

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GM.Identity.Sample");

        var url = provider.UserDetailsEndpoint!;
        if (provider.UserInfoTokenPlacement == UserInfoTokenPlacement.Query)
        {
            var separator = url.Contains('?') ? '&' : '?';
            url = $"{url}{separator}access_token={Uri.EscapeDataString(accessToken)}";
        }
        else
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new CustomException($"Failed to fetch user info, statusCode: {(int)response.StatusCode}");

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(payload);

        // GitHub's /user/emails returns an array of { email, primary, verified }.
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            return PickPrimaryEmail(doc.RootElement);

        return doc.RootElement.TryGetProperty(provider.EmailField, out var field)
            ? field.GetString()
            : null;
    }

    // From an array of email records, prefer the primary+verified address, then any verified, then the first.
    private static string? PickPrimaryEmail(JsonElement emails)
    {
        string? firstVerified = null;
        string? first = null;

        foreach (var entry in emails.EnumerateArray())
        {
            if (!entry.TryGetProperty("email", out var emailProp)) continue;
            var email = emailProp.GetString();
            if (string.IsNullOrWhiteSpace(email)) continue;

            first ??= email;
            var verified = entry.TryGetProperty("verified", out var v) && v.ValueKind == JsonValueKind.True;
            var primary = entry.TryGetProperty("primary", out var p) && p.ValueKind == JsonValueKind.True;

            if (verified && primary) return email;
            if (verified) firstVerified ??= email;
        }

        return firstVerified ?? first;
    }

    private OAuthProviderOptions ResolveProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider) || !_providers.TryGetValue(provider, out var options))
            throw new CustomException($"Unknown external identity provider '{provider}'.");
        return options;
    }
}
