using GM.Identity.Sample.Application.Users.Commands.CreateApiKey;
using GM.Identity.Sample.Application.Users.Commands.RevokeApiKey;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ApiKeyAggregate;
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
/// End-to-end proof of personal access tokens (API keys): a created key authenticates non-interactively via
/// grant_type=api_key, and once revoked it no longer works. Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class ApiKeyEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private Guid _userId;
    private Guid _apiKeyId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create($"apikey-{Guid.NewGuid():N}", $"apikey-{Guid.NewGuid():N}@test.local", null);
            var (hash, salt) = PasswordHasher.Hash("Correct123!");
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
                await context.Set<ApiKey>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<UserSession>().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task An_api_key_authenticates_and_stops_working_once_revoked()
    {
        if (!_infraReady) return;

        string apiKey;
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            apiKey = await mediator.Send(new CreateApiKeyCommand { UserId = _userId, Name = "ci" });

            // Capture the key id for revocation.
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _apiKeyId = await context.Set<ApiKey>().IgnoreQueryFilters().Where(x => x.UserId == _userId)
                .Select(x => x.Id).FirstAsync();
        }

        // The key logs in via grant_type=api_key.
        var login = await TokenAsync(
            ("grant_type", "api_key"), ("api_key", apiKey), ("client_id", ClientId), ("client_secret", ClientSecret));
        await login.ShouldBeOkAsync();
        var tokens = JsonSerializer.Deserialize<TokenResponse>(await login.Content.ReadAsStringAsync(), JsonOptions);
        Assert.False(string.IsNullOrEmpty(tokens!.AccessToken));

        // Revoke it → it no longer authenticates.
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new RevokeApiKeyCommand { UserId = _userId, ApiKeyId = _apiKeyId });
        }

        var afterRevoke = await TokenAsync(
            ("grant_type", "api_key"), ("api_key", apiKey), ("client_id", ClientId), ("client_secret", ClientSecret));
        Assert.False(afterRevoke.IsSuccessStatusCode);
    }

    private Task<HttpResponseMessage> TokenAsync(params (string Key, string Value)[] fields)
    {
        var content = new FormUrlEncodedContent(
            fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));
        return _factory.CreateClient().PostAsync("/connect/token", content);
    }

    private sealed record TokenResponse(
        string? AccessToken, DateTime? ExpiresAt, string TokenType, string? RefreshToken, DateTime? RefreshTokenExpiresAt);
}
