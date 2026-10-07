using GM.Identity.Sample.Application.Users.Commands.SetUserActive;
using GM.Identity.Sample.Application.Users.Commands.SetUserBlock;
using GM.Identity.Sample.Application.Users.Commands.UnlockUser;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
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
/// End-to-end proof of the user state-management security properties: blocking or deactivating a user rejects
/// future logins AND revokes their live sessions immediately (the old refresh token stops working), and
/// unlocking clears a failed-login lockout so the correct password works again. State changes are dispatched
/// through <see cref="IMediator"/> (the real commands, including session revocation + cache eviction).
/// Requires PostgreSQL + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class UserStateEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly List<Guid> _userIds = new();
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // Touch the DB so an unavailable Postgres/Redis flips _infraReady off up front.
            await context.Database.CanConnectAsync();
            _infraReady = true;
        }
        catch
        {
            _infraReady = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_infraReady && _userIds.Count > 0)
        {
            try
            {
                using var scope = _factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Set<UserSession>().Where(s => s.UserId != null && _userIds.Contains(s.UserId!.Value))
                    .ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => _userIds.Contains(u.Id))
                    .ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Blocking_a_user_rejects_login_and_revokes_live_sessions()
    {
        if (!_infraReady) return;

        var (userName, _) = await CreateUserAsync();

        // The user is logged in with a live refresh token.
        var login = await ReadTokenAsync(await PasswordGrantAsync(userName));
        var refreshToken = login!.RefreshToken!;

        await SendAsync(new SetUserBlockCommand { UserId = _userIds[^1], Block = true });

        // A blocked user cannot obtain new tokens...
        var afterBlock = await PasswordGrantAsync(userName);
        Assert.False(afterBlock.IsSuccessStatusCode);

        // ...and the session they already held is revoked, so the old refresh token no longer works.
        var refreshAfterBlock = await RefreshGrantAsync(refreshToken);
        Assert.False(refreshAfterBlock.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Unblocking_a_user_restores_login()
    {
        if (!_infraReady) return;

        var (userName, _) = await CreateUserAsync();
        var userId = _userIds[^1];

        await SendAsync(new SetUserBlockCommand { UserId = userId, Block = true });
        Assert.False((await PasswordGrantAsync(userName)).IsSuccessStatusCode);

        await SendAsync(new SetUserBlockCommand { UserId = userId, Block = false });
        await (await PasswordGrantAsync(userName)).ShouldBeOkAsync();
    }

    [Fact]
    public async Task Unlocking_a_user_clears_the_failed_login_lockout()
    {
        if (!_infraReady) return;

        var (userName, _) = await CreateUserAsync();
        var userId = _userIds[^1];

        // Trip the lockout (MaxFailedAccessAttempts = 5).
        for (var attempt = 0; attempt < 5; attempt++)
            Assert.False((await PasswordGrantAsync(userName, "wrong-password")).IsSuccessStatusCode);

        // Correct password is still rejected while locked out.
        Assert.False((await PasswordGrantAsync(userName)).IsSuccessStatusCode);

        // Unlock clears the lockout and the failed-attempt counter, so the correct password works again.
        await SendAsync(new UnlockUserCommand { UserId = userId });
        await (await PasswordGrantAsync(userName)).ShouldBeOkAsync();
    }

    [Fact]
    public async Task Deactivating_a_user_rejects_login_and_revokes_live_sessions()
    {
        if (!_infraReady) return;

        var (userName, _) = await CreateUserAsync();
        var userId = _userIds[^1];

        var login = await ReadTokenAsync(await PasswordGrantAsync(userName));
        var refreshToken = login!.RefreshToken!;

        await SendAsync(new SetUserActiveCommand { UserId = userId, Active = false });

        Assert.False((await PasswordGrantAsync(userName)).IsSuccessStatusCode);
        Assert.False((await RefreshGrantAsync(refreshToken)).IsSuccessStatusCode);

        // Reactivating restores login.
        await SendAsync(new SetUserActiveCommand { UserId = userId, Active = true });
        await (await PasswordGrantAsync(userName)).ShouldBeOkAsync();
    }

    private async Task<(string UserName, Guid Id)> CreateUserAsync()
    {
        var userName = $"userstate-{Guid.NewGuid():N}";
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = User.Create(userName, $"{userName}@test.local", null);
        var (hash, salt) = PasswordHasher.Hash(Password);
        user.UpdatePassword(hash, salt);
        await context.Set<User>().AddAsync(user);
        await context.SaveChangesAsync();
        _userIds.Add(user.Id);
        return (userName, user.Id);
    }

    private async Task SendAsync(IRequest request)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(request);
    }

    private Task<HttpResponseMessage> PasswordGrantAsync(string userName, string? password = null) =>
        TokenAsync(
            ("grant_type", "password"), ("username", userName), ("password", password ?? Password),
            ("client_id", ClientId), ("client_secret", ClientSecret));

    private Task<HttpResponseMessage> RefreshGrantAsync(string refreshToken) =>
        TokenAsync(
            ("grant_type", "refresh_token"), ("refresh_token", refreshToken),
            ("client_id", ClientId), ("client_secret", ClientSecret));

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
        string? AccessToken, DateTime? ExpiresAt, string TokenType, string? RefreshToken, DateTime? RefreshTokenExpiresAt);
}
