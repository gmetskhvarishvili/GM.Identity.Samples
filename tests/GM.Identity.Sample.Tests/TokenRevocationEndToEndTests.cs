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
using System.Text.Json;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end tests for the OAuth token-revocation endpoint (RFC 7009, <c>/connect/revoke</c>): revoking a
/// token kills its session (a subsequent refresh fails), and revoking an unknown token is a no-op that still
/// returns 200. Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class TokenRevocationEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"revoke-{Guid.NewGuid():N}";
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
    public async Task Revoking_an_access_token_kills_the_session()
    {
        if (!_infraReady) return;

        var login = await ReadTokenAsync(await PasswordGrantAsync());
        var accessToken = login!.AccessToken!;
        var refreshToken = login.RefreshToken!;

        var revoke = await RevokeAsync(accessToken);
        await revoke.ShouldBeOkAsync();

        // The session is gone — its refresh token no longer rotates into a new one.
        var refreshAfter = await TokenAsync(
            ("grant_type", "refresh_token"), ("refresh_token", refreshToken),
            ("client_id", ClientId), ("client_secret", ClientSecret));
        Assert.False(refreshAfter.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Revoking_an_unknown_token_is_a_no_op_and_succeeds()
    {
        if (!_infraReady) return;

        var revoke = await RevokeAsync("this-is-not-a-real-token");
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
    }

    private Task<HttpResponseMessage> PasswordGrantAsync() =>
        TokenAsync(
            ("grant_type", "password"), ("username", _userName), ("password", Password),
            ("client_id", ClientId), ("client_secret", ClientSecret));

    private Task<HttpResponseMessage> RevokeAsync(string token) =>
        TokenEndpointAsync("/connect/revoke",
            ("token", token), ("client_id", ClientId), ("client_secret", ClientSecret));

    private Task<HttpResponseMessage> TokenAsync(params (string Key, string Value)[] fields) =>
        TokenEndpointAsync("/connect/token", fields);

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
}
