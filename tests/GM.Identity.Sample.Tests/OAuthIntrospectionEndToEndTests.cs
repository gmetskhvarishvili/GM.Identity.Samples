using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end tests for the OAuth introspection (RFC 7662, <c>/connect/introspect</c>) and OIDC userinfo
/// (<c>/connect/userinfo</c>) endpoints against opaque tokens. Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class OAuthIntrospectionEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"introspect-{Guid.NewGuid():N}";
    private Guid _userId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create(_userName, $"{_userName}@test.local", null);
            var (hash, salt) = PasswordHasher.Hash(Password);
            user.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(user);
            await context.SaveChangesAsync();
            _userId = user.Id;
            _infraReady = true;
        }
        catch
        {
            _infraReady = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_infraReady)
        {
            try
            {
                using var scope = _factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Set<UserSession>().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Introspecting_a_live_access_token_reports_it_active()
    {
        if (!_infraReady) return;

        var login = await ReadTokenAsync(await PasswordGrantAsync());

        var result = await IntrospectAsync(login!.AccessToken!);
        Assert.True(result!.Active);
        Assert.Equal(_userId.ToString(), result.Subject);
        Assert.Equal("access_token", result.TokenType);
        Assert.Equal(ClientId, result.ClientId, ignoreCase: true);
        Assert.NotNull(result.Expiry);

        // The refresh token introspects as active too, distinguished by token_type.
        var refreshResult = await IntrospectAsync(login.RefreshToken!);
        Assert.True(refreshResult!.Active);
        Assert.Equal("refresh_token", refreshResult.TokenType);
    }

    [Fact]
    public async Task Introspecting_an_unknown_or_revoked_token_reports_it_inactive()
    {
        if (!_infraReady) return;

        var unknown = await IntrospectAsync("not-a-real-token");
        Assert.False(unknown!.Active);

        // A revoked token introspects as inactive.
        var login = await ReadTokenAsync(await PasswordGrantAsync());
        var revoke = await TokenEndpointAsync("/connect/revoke",
            ("token", login!.AccessToken!), ("client_id", ClientId), ("client_secret", ClientSecret));
        await revoke.ShouldBeOkAsync();

        var revoked = await IntrospectAsync(login.AccessToken!);
        Assert.False(revoked!.Active);
    }

    [Fact]
    public async Task Userinfo_returns_the_claims_of_the_bearer_tokens_user()
    {
        if (!_infraReady) return;

        var login = await ReadTokenAsync(await PasswordGrantAsync());

        var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        var response = await _factory.CreateClient().SendAsync(request);
        await response.ShouldBeOkAsync();

        var info = JsonSerializer.Deserialize<UserInfoResponse>(
            await response.Content.ReadAsStringAsync(), JsonOptions);
        Assert.Equal(_userId.ToString(), info!.Subject);
        Assert.Equal(_userName, info.PreferredUsername);
        Assert.Equal($"{_userName}@test.local", info.Email);
    }

    [Fact]
    public async Task Userinfo_with_an_invalid_token_is_unauthorized()
    {
        if (!_infraReady) return;

        var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "garbage");
        var response = await _factory.CreateClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> PasswordGrantAsync() =>
        TokenEndpointAsync("/connect/token",
            ("grant_type", "password"), ("username", _userName), ("password", Password),
            ("client_id", ClientId), ("client_secret", ClientSecret));

    private async Task<IntrospectionResponse?> IntrospectAsync(string token)
    {
        var response = await TokenEndpointAsync("/connect/introspect",
            ("token", token), ("client_id", ClientId), ("client_secret", ClientSecret));
        await response.ShouldBeOkAsync();
        return JsonSerializer.Deserialize<IntrospectionResponse>(
            await response.Content.ReadAsStringAsync(), JsonOptions);
    }

    private Task<HttpResponseMessage> TokenEndpointAsync(string path, params (string Key, string Value)[] fields)
    {
        var content = new FormUrlEncodedContent(
            fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));
        return _factory.CreateClient().PostAsync(path, content);
    }

    private static async Task<TokenResponse?> ReadTokenAsync(HttpResponseMessage response)
    {
        await response.ShouldBeOkAsync();
        return JsonSerializer.Deserialize<TokenResponse>(
            await response.Content.ReadAsStringAsync(), JsonOptions);
    }

    private sealed record TokenResponse(
        string? AccessToken, DateTime? ExpiresAt, string TokenType, string? RefreshToken, DateTime? RefreshTokenExpiresAt);

    private sealed record IntrospectionResponse(
        [property: JsonPropertyName("active")] bool Active,
        [property: JsonPropertyName("sub")] string? Subject,
        [property: JsonPropertyName("client_id")] string? ClientId,
        [property: JsonPropertyName("token_type")] string? TokenType,
        [property: JsonPropertyName("exp")] long? Expiry,
        [property: JsonPropertyName("scope")] string? Scope);

    private sealed record UserInfoResponse(
        [property: JsonPropertyName("sub")] string Subject,
        [property: JsonPropertyName("preferred_username")] string PreferredUsername,
        [property: JsonPropertyName("email")] string? Email);
}
