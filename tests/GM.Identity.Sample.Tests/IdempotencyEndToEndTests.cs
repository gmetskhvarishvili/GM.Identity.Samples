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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// The <c>Idempotency-Key</c> header is safe to send on every call: an ordinary write with a repeated key is
/// replayed (same result, <c>Idempotency-Replayed</c> header), while <c>/connect/*</c> ignores the key, so a
/// token response is never stored for replay and each call issues fresh tokens. Requires PostgreSQL + Redis;
/// no-ops if they aren't reachable.
/// </summary>
public sealed class IdempotencyEndToEndTests : IAsyncLifetime
{
    // Matches the seeded client (Seed:ClientId / Seed:ClientSecret in appsettings).
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";
    private const string ReplayedHeader = "Idempotency-Replayed";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _loginUserName = $"idem-login-{Guid.NewGuid():N}";
    private readonly string _signUpUserName = $"idem-signup-{Guid.NewGuid():N}";
    private Guid _loginUserId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create(_loginUserName, $"{_loginUserName}@test.local", null);
            var (hash, salt) = PasswordHasher.Hash(Password);
            user.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(user);
            await context.SaveChangesAsync();
            _loginUserId = user.Id;
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
                await context.Set<User>()
                    .IgnoreQueryFilters()
                    .Where(u => u.Id == _loginUserId || u.UserName == _signUpUserName)
                    .ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Repeated_key_on_a_write_replays_the_first_response()
    {
        if (!_infraReady) return;
        var key = Guid.NewGuid().ToString();

        var first = await SignUpAsync(key);
        var second = await SignUpAsync(key);

        await first.ShouldBeOkAsync();
        await second.ShouldBeOkAsync();
        Assert.False(first.Headers.Contains(ReplayedHeader));
        Assert.True(second.Headers.Contains(ReplayedHeader));
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Repeated_key_on_the_token_endpoint_is_ignored_and_issues_fresh_tokens()
    {
        if (!_infraReady) return;
        var key = Guid.NewGuid().ToString();

        var first = await LoginAsync(key);
        var second = await LoginAsync(key);

        await first.ShouldBeOkAsync();
        await second.ShouldBeOkAsync();
        Assert.False(second.Headers.Contains(ReplayedHeader));
        Assert.NotEqual(await AccessTokenAsync(first), await AccessTokenAsync(second));
    }

    private async Task<HttpResponseMessage> SignUpAsync(string idempotencyKey)
    {
        var body = JsonSerializer.Serialize(new
        {
            username = _signUpUserName,
            email = $"{_signUpUserName}@test.local",
            password = Password,
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Users")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await _factory.CreateClient().SendAsync(request);
    }

    private async Task<HttpResponseMessage> LoginAsync(string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = _loginUserName,
                ["password"] = Password,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
            }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await _factory.CreateClient().SendAsync(request);
    }

    private static async Task<string> AccessTokenAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("accessToken").GetString()!;
    }
}
