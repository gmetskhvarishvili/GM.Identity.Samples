using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPasswordHistoryAggregate;
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
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of the password-expiry policy: with Auth:PasswordExpiryDays = 1, a user whose most recent
/// password change is older than that is refused login until they set a newer password. Requires Postgres +
/// Redis; no-ops if unavailable.
/// </summary>
public sealed class PasswordExpiryEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private readonly GmWebApplicationFactory<Program> _factory =
        new GmWebApplicationFactory<Program>().WithConfig("Auth:PasswordExpiryDays", "1");

    private readonly string _userName = $"expiry-{Guid.NewGuid():N}";
    private Guid _userId;
    private string _hash = null!;
    private string _salt = null!;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create(_userName, $"{_userName}@test.local", null);
            (_hash, _salt) = PasswordHasher.Hash(Password);
            user.UpdatePassword(_hash, _salt);
            await context.Set<User>().AddAsync(user);
            // Only an OLD password-change record exists → the password is expired.
            await context.Set<UserPasswordHistory>()
                .AddAsync(UserPasswordHistory.Create(user.Id, _hash, _salt, DateTime.UtcNow.AddDays(-2)));
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
                await context.Set<UserPasswordHistory>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Expired_password_is_refused_until_a_newer_one_is_recorded()
    {
        if (!_infraReady) return;

        // The correct password is refused because it is older than the expiry window.
        Assert.False((await PasswordGrantAsync()).IsSuccessStatusCode);

        // Record a fresh password change → login is allowed again.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Set<UserPasswordHistory>()
                .AddAsync(UserPasswordHistory.Create(_userId, _hash, _salt, DateTime.UtcNow));
            await context.SaveChangesAsync();
        }

        await (await PasswordGrantAsync()).ShouldBeOkAsync();
    }

    private Task<HttpResponseMessage> PasswordGrantAsync()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", _userName),
            new KeyValuePair<string, string>("password", Password),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("client_secret", ClientSecret),
        });
        return _factory.CreateClient().PostAsync("/connect/token", content);
    }
}
