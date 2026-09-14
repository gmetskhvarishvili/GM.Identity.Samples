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
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end tests (GM.Testing <see cref="GmWebApplicationFactory{T}"/>) for the token endpoint's
/// refresh-token rotation, reuse detection, and failed-login lockout. Uses the seeded OAuth client and
/// dedicated throwaway users (so the shared admin is never contaminated). Requires PostgreSQL + Redis;
/// no-ops if they aren't reachable.
/// </summary>
public sealed class AuthenticationEndToEndTests : IAsyncLifetime
{
    // Matches the seeded client (Seed:ClientId / Seed:ClientSecret in appsettings).
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _loginUserName = $"auth-login-{Guid.NewGuid():N}";
    private readonly string _lockoutUserName = $"auth-lockout-{Guid.NewGuid():N}";
    private Guid _loginUserId;
    private Guid _lockoutUserId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _loginUserId = await CreateUserAsync(context, _loginUserName);
            _lockoutUserId = await CreateUserAsync(context, _lockoutUserName);
            _infraReady = true;
        }
        catch
        {
            _infraReady = false; // Postgres/Redis unavailable — tests will no-op.
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
                await context.Set<UserSession>()
                    .Where(s => s.UserId == _loginUserId || s.UserId == _lockoutUserId)
                    .ExecuteDeleteAsync();
                await context.Set<User>()
                    .Where(u => u.Id == _loginUserId || u.Id == _lockoutUserId)
                    .ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Password_grant_returns_an_access_and_refresh_token()
    {
        if (!_infraReady) return;

        var response = await TokenAsync(
            ("grant_type", "password"), ("username", _loginUserName), ("password", Password),
            ("client_id", ClientId), ("client_secret", ClientSecret));

        await response.ShouldBeOkAsync();
        var token = await ReadTokenAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(token!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(token.RefreshToken));
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        if (!_infraReady) return;

        var response = await TokenAsync(
            ("grant_type", "password"), ("username", _loginUserName), ("password", "wrong-password"),
            ("client_id", ClientId), ("client_secret", ClientSecret));

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_reuse_revokes_all_sessions()
    {
        if (!_infraReady) return;

        // Log in and grab the first refresh token.
        var login = await ReadTokenAsync(await TokenAsync(
            ("grant_type", "password"), ("username", _loginUserName), ("password", Password),
            ("client_id", ClientId), ("client_secret", ClientSecret)));
        var refresh1 = login!.RefreshToken!;

        // Rotate: refresh1 → a new, different refresh token.
        var rotateResponse = await TokenAsync(
            ("grant_type", "refresh_token"), ("refresh_token", refresh1),
            ("client_id", ClientId), ("client_secret", ClientSecret));
        await rotateResponse.ShouldBeOkAsync();
        var refresh2 = (await ReadTokenAsync(rotateResponse))!.RefreshToken!;
        Assert.NotEqual(refresh1, refresh2);

        // Replaying the already-rotated refresh1 is a reuse → rejected AND revokes every session.
        var reuse = await TokenAsync(
            ("grant_type", "refresh_token"), ("refresh_token", refresh1),
            ("client_id", ClientId), ("client_secret", ClientSecret));
        Assert.False(reuse.IsSuccessStatusCode);

        // Because reuse revoked all sessions, the freshly-rotated refresh2 is now invalid too.
        var afterReuse = await TokenAsync(
            ("grant_type", "refresh_token"), ("refresh_token", refresh2),
            ("client_id", ClientId), ("client_secret", ClientSecret));
        Assert.False(afterReuse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Account_locks_after_repeated_failed_logins()
    {
        if (!_infraReady) return;

        // Five consecutive bad passwords trip the lockout (MaxFailedAccessAttempts = 5).
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var bad = await TokenAsync(
                ("grant_type", "password"), ("username", _lockoutUserName), ("password", "wrong-password"),
                ("client_id", ClientId), ("client_secret", ClientSecret));
            Assert.False(bad.IsSuccessStatusCode);
        }

        // Now the correct password is still rejected while the lockout window is active.
        var afterLock = await TokenAsync(
            ("grant_type", "password"), ("username", _lockoutUserName), ("password", Password),
            ("client_id", ClientId), ("client_secret", ClientSecret));
        Assert.False(afterLock.IsSuccessStatusCode);
    }

    private static async Task<Guid> CreateUserAsync(ApplicationDbContext context, string userName)
    {
        var user = User.Create(userName, $"{userName}@test.local", null);
        var (hash, salt) = PasswordHasher.Hash(Password);
        user.UpdatePassword(hash, salt);
        await context.Set<User>().AddAsync(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    private Task<HttpResponseMessage> TokenAsync(params (string Key, string Value)[] fields)
    {
        var content = new FormUrlEncodedContent(
            fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));
        return _factory.CreateClient().PostAsync("/connect/token", content);
    }

    private static async Task<TokenResponse?> ReadTokenAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<TokenResponse>(
            await response.Content.ReadAsStringAsync(), JsonOptions);

    private sealed record TokenResponse(
        string AccessToken, DateTime ExpiresAt, string TokenType, string? RefreshToken, DateTime? RefreshTokenExpiresAt);
}
