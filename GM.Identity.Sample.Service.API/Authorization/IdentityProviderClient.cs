using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace GM.Identity.Sample.Service.API.Authorization;

/// <summary>
/// All calls this resource server makes to the Identity provider over HTTP. The provider is the source of
/// truth: it validates tokens (RFC 7662 introspection) and decides permissions and scopes. This service holds
/// no session / RBAC / scope state of its own.
/// </summary>
public interface IIdentityProviderClient
{
    Task<IntrospectionResult> IntrospectAsync(string token, CancellationToken cancellationToken);
    Task<bool> HasPermissionAsync(Guid userId, string name, CancellationToken cancellationToken);
    Task<bool> HasScopeAsync(Guid clientId, string operation, CancellationToken cancellationToken);
}

internal sealed class IdentityProviderClient(
    HttpClient httpClient, IConfiguration configuration, IMemoryCache cache) : IIdentityProviderClient
{
    private readonly string _clientId =
        configuration["IdentityApi:ClientId"]
        ?? throw new InvalidOperationException("IdentityApi:ClientId is required for token introspection.");

    private readonly string _clientSecret =
        configuration["IdentityApi:ClientSecret"]
        ?? throw new InvalidOperationException("IdentityApi:ClientSecret is required for token introspection.");

    // Short-lived cache so a hot token isn't introspected on every request. Bounds how long a revoked token
    // can still pass, so keep it small (default 30s). Capped by the token's own expiry.
    private readonly TimeSpan _cacheTtl =
        TimeSpan.FromSeconds(Math.Max(0, configuration.GetValue("IdentityApi:IntrospectionCacheSeconds", 30)));

    public async Task<IntrospectionResult> IntrospectAsync(string token, CancellationToken cancellationToken)
    {
        var key = "introspect:" + Hash(token);
        if (_cacheTtl > TimeSpan.Zero && cache.TryGetValue<IntrospectionResult>(key, out var cached))
            return cached!;

        IntrospectionResult result;
        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = token,
                ["token_type_hint"] = "access_token",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
            });

            using var response = await httpClient.PostAsync("connect/introspect", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return IntrospectionResult.Inactive; // transient failure — do not cache

            result = await response.Content.ReadFromJsonAsync<IntrospectionResult>(cancellationToken)
                     ?? IntrospectionResult.Inactive;
        }
        catch (HttpRequestException)
        {
            return IntrospectionResult.Inactive; // provider unreachable — do not cache
        }

        // Cache the genuine provider answer (active or inactive), never longer than the token's own lifetime.
        var ttl = _cacheTtl;
        if (result.Active && result.Exp is { } exp)
        {
            var remaining = DateTimeOffset.FromUnixTimeSeconds(exp) - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) return result; // already expired — don't cache
            if (remaining < ttl) ttl = remaining;
        }

        if (ttl > TimeSpan.Zero)
            cache.Set(key, result, ttl);

        return result;
    }

    public Task<bool> HasPermissionAsync(Guid userId, string name, CancellationToken cancellationToken) =>
        CheckAsync($"connect/authz/permission?userId={userId}&name={Uri.EscapeDataString(name)}", cancellationToken);

    public Task<bool> HasScopeAsync(Guid clientId, string operation, CancellationToken cancellationToken) =>
        CheckAsync($"connect/authz/scope?clientId={clientId}&operation={Uri.EscapeDataString(operation)}", cancellationToken);

    private async Task<bool> CheckAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(relativeUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return false; // fail closed on any provider error

            var decision = await response.Content.ReadFromJsonAsync<AuthorizationDecision>(cancellationToken);
            return decision?.Granted ?? false;
        }
        catch (HttpRequestException)
        {
            return false; // provider unreachable → deny
        }
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

/// <summary>The provider's RFC 7662 introspection response (the fields this service uses).</summary>
public sealed record IntrospectionResult(
    [property: JsonPropertyName("active")] bool Active,
    [property: JsonPropertyName("sub")] string? Sub,
    [property: JsonPropertyName("client_id")] string? ClientId,
    [property: JsonPropertyName("scope")] string? Scope,
    [property: JsonPropertyName("exp")] long? Exp)
{
    public static readonly IntrospectionResult Inactive = new(false, null, null, null, null);
}

/// <summary>The provider's authorization decision.</summary>
public sealed record AuthorizationDecision(bool Granted);
