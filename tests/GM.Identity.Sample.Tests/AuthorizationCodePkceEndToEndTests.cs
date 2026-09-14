using GM.Identity.Sample.Application.Common.Security;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate;
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
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of the PKCE authorization-code flow: /connect/authorize mints a code bound to the client,
/// redirect URI and code challenge; /connect/token (grant_type=authorization_code) exchanges it for a session
/// only when the code verifier matches; the code is single-use; and an unregistered redirect URI is rejected.
/// Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class AuthorizationCodePkceEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"authcode-{Guid.NewGuid():N}";
    private readonly string _redirectUri = $"https://client.example/callback/{Guid.NewGuid():N}";
    private readonly Guid _clientId = Guid.Parse(ClientId);
    private Guid _userId;
    private Guid _redirectUriId;
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

            var redirect = ClientRedirectUri.Create(_clientId, _redirectUri);
            await context.Set<ClientRedirectUri>().AddAsync(redirect);
            await context.SaveChangesAsync();
            _userId = user.Id;
            _redirectUriId = redirect.Id;
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
                await context.Set<AuthorizationCode>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<ClientRedirectUri>().IgnoreQueryFilters().Where(x => x.Id == _redirectUriId).ExecuteDeleteAsync();
                await context.Set<UserSession>().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Full_pkce_flow_issues_a_session_and_the_code_is_single_use()
    {
        if (!_infraReady) return;

        var verifier = PkceHelper.GenerateCodeVerifier();
        var challenge = PkceHelper.GenerateCodeChallenge(verifier);

        // Authorize → code.
        var authorize = await AuthorizeAsync(challenge);
        await authorize.ShouldBeOkAsync();
        var code = (await ReadAuthorizeAsync(authorize))!.Code;
        Assert.False(string.IsNullOrWhiteSpace(code));

        // Exchange the code (with the matching verifier) → tokens.
        var exchange = await ExchangeAsync(code, verifier);
        await exchange.ShouldBeOkAsync();
        var tokens = await ReadTokenAsync(exchange);
        Assert.False(string.IsNullOrEmpty(tokens!.AccessToken));
        Assert.False(string.IsNullOrEmpty(tokens.RefreshToken));

        // The code is single-use: replaying it fails.
        var replay = await ExchangeAsync(code, verifier);
        Assert.False(replay.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Exchange_with_a_wrong_verifier_is_rejected()
    {
        if (!_infraReady) return;

        var verifier = PkceHelper.GenerateCodeVerifier();
        var challenge = PkceHelper.GenerateCodeChallenge(verifier);

        var code = (await ReadAuthorizeAsync(await AuthorizeAsync(challenge)))!.Code;

        var exchange = await ExchangeAsync(code, PkceHelper.GenerateCodeVerifier()); // different verifier
        Assert.False(exchange.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Authorize_with_an_unregistered_redirect_uri_is_rejected()
    {
        if (!_infraReady) return;

        var verifier = PkceHelper.GenerateCodeVerifier();
        var challenge = PkceHelper.GenerateCodeChallenge(verifier);

        var response = await PostAsync("/connect/authorize",
            ("client_id", ClientId), ("redirect_uri", "https://attacker.example/callback"),
            ("code_challenge", challenge), ("code_challenge_method", "S256"),
            ("username", _userName), ("password", Password));
        Assert.False(response.IsSuccessStatusCode);
    }

    private Task<HttpResponseMessage> AuthorizeAsync(string challenge) =>
        PostAsync("/connect/authorize",
            ("client_id", ClientId), ("redirect_uri", _redirectUri),
            ("code_challenge", challenge), ("code_challenge_method", "S256"),
            ("state", "xyz"), ("username", _userName), ("password", Password));

    private Task<HttpResponseMessage> ExchangeAsync(string code, string verifier) =>
        PostAsync("/connect/token",
            ("grant_type", "authorization_code"), ("code", code), ("redirect_uri", _redirectUri),
            ("code_verifier", verifier), ("client_id", ClientId), ("client_secret", ClientSecret));

    private Task<HttpResponseMessage> PostAsync(string path, params (string Key, string Value)[] fields)
    {
        var content = new FormUrlEncodedContent(
            fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));
        return _factory.CreateClient().PostAsync(path, content);
    }

    private static async Task<AuthorizeResponse?> ReadAuthorizeAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<AuthorizeResponse>(await response.Content.ReadAsStringAsync(), JsonOptions);

    private static async Task<TokenResponse?> ReadTokenAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync(), JsonOptions);

    private sealed record AuthorizeResponse(string Code, string? State, string RedirectTo);

    private sealed record TokenResponse(
        string? AccessToken, DateTime? ExpiresAt, string TokenType, string? RefreshToken, DateTime? RefreshTokenExpiresAt);
}
