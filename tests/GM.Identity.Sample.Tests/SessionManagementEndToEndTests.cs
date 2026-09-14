using GM.Identity.Sample.Application.Users.Commands.LogoutAllUserSessions;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Infrastructure.Maintenance;
using GM.Identity.Sample.Persistence.Context;
using GM.Identity.Authorization;
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
/// End-to-end tests for session management &amp; hygiene: "log out everywhere" revokes every active session,
/// and the expired-artifacts purge job reaps dead sessions and spent/expired authorization codes. Requires
/// Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class SessionManagementEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"session-{Guid.NewGuid():N}";
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
                await context.Set<AuthorizationCode>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<UserSession>().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Logout_everywhere_revokes_all_of_the_users_sessions()
    {
        if (!_infraReady) return;

        // Two independent logins → two live sessions.
        var first = await ReadTokenAsync(await PasswordGrantAsync());
        var second = await ReadTokenAsync(await PasswordGrantAsync());

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new LogoutAllUserSessionsCommand { UserId = _userId });
        }

        // Both refresh tokens are now dead.
        Assert.False((await RefreshAsync(first!.RefreshToken!)).IsSuccessStatusCode);
        Assert.False((await RefreshAsync(second!.RefreshToken!)).IsSuccessStatusCode);
    }

    [Fact]
    public async Task Purge_job_reaps_dead_sessions_and_spent_authorization_codes()
    {
        if (!_infraReady) return;

        var past = DateTime.UtcNow.AddDays(-2);

        // Seed an already-expired session and an already-expired authorization code.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var deadSession = UserSession.Create(
                _userId, Guid.Parse(ClientId), null, TokenGenerator.Hash(Guid.NewGuid().ToString()),
                past, TokenGenerator.Hash(Guid.NewGuid().ToString()));
            await context.Set<UserSession>().AddAsync(deadSession);

            var expiredCode = AuthorizationCode.Create(
                Guid.Parse(ClientId), _userId, TokenGenerator.Hash(Guid.NewGuid().ToString()),
                "https://client.example/cb", null, "challenge", "S256", past);
            await context.Set<AuthorizationCode>().AddAsync(expiredCode);
            await context.SaveChangesAsync();
        }

        // Run the purge job.
        using (var scope = _factory.Services.CreateScope())
        {
            var job = ActivatorUtilities.CreateInstance<ExpiredArtifactsPurgeJob>(scope.ServiceProvider);
            await job.ExecuteAsync(null!, default);
        }

        // The dead rows are gone.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await context.Set<UserSession>().IgnoreQueryFilters().AnyAsync(s => s.UserId == _userId));
            Assert.False(await context.Set<AuthorizationCode>().IgnoreQueryFilters().AnyAsync(c => c.UserId == _userId));
        }
    }

    private Task<HttpResponseMessage> PasswordGrantAsync() =>
        TokenAsync(
            ("grant_type", "password"), ("username", _userName), ("password", Password),
            ("client_id", ClientId), ("client_secret", ClientSecret));

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        TokenAsync(
            ("grant_type", "refresh_token"), ("refresh_token", refreshToken),
            ("client_id", ClientId), ("client_secret", ClientSecret));

    private Task<HttpResponseMessage> TokenAsync(params (string Key, string Value)[] fields)
    {
        var content = new FormUrlEncodedContent(
            fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));
        return _factory.CreateClient().PostAsync("/connect/token", content);
    }

    private static async Task<TokenResponse?> ReadTokenAsync(HttpResponseMessage response)
    {
        await response.ShouldBeOkAsync();
        return JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync(), JsonOptions);
    }

    private sealed record TokenResponse(
        string? AccessToken, DateTime? ExpiresAt, string TokenType, string? RefreshToken, DateTime? RefreshTokenExpiresAt);
}
