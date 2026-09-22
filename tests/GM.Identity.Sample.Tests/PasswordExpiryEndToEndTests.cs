using GM.EntityFramework.Persistence.Events;
using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.Events.Users;
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
/// <see cref="UserPasswordChangedDomainEvent"/> is older than that is refused login until a newer password
/// change is recorded. Expiry is derived entirely from the domain-event log. Requires Postgres + Redis;
/// no-ops if unavailable.
/// </summary>
public sealed class PasswordExpiryEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";
    private static readonly string PasswordChangedEventType = nameof(UserPasswordChangedDomainEvent);

    private readonly GmWebApplicationFactory<Program> _factory =
        new GmWebApplicationFactory<Program>().WithConfig("Auth:PasswordExpiryDays", "1");

    private readonly string _userName = $"expiry-{Guid.NewGuid():N}";
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
            _userId = user.Id;

            // Only an OLD password-change event exists → the password is expired.
            context.DomainEvents.Add(PasswordChangedEvent(_userId, DateTime.UtcNow.AddDays(-2)));
            await context.SaveChangesAsync();
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
                await context.DomainEvents
                    .Where(x => x.AggregateType == "User" && x.AggregateId == _userId.ToString())
                    .ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Expired_password_is_refused_until_a_newer_change_is_recorded()
    {
        if (!_infraReady) return;

        // The correct password is refused because the last change is older than the expiry window.
        Assert.False((await PasswordGrantAsync()).IsSuccessStatusCode);

        // Record a fresh password-change event → login is allowed again.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.DomainEvents.Add(PasswordChangedEvent(_userId, DateTime.UtcNow));
            await context.SaveChangesAsync();
        }

        await (await PasswordGrantAsync()).ShouldBeOkAsync();
    }

    private static StoredDomainEvent PasswordChangedEvent(Guid userId, DateTime occurredOn) => new()
    {
        Id = Guid.NewGuid(),
        AggregateType = "User",
        AggregateId = userId.ToString(),
        EventType = PasswordChangedEventType,
        Payload = "{}",
        OccurredOn = occurredOn,
        UserId = userId,
    };

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
