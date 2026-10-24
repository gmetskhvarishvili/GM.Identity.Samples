using GM.Identity.Sample.Domain.BoundedContext.AuditBoundedContext.AuditAggregate;
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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// Sign-in and sign-up arrive anonymous (no gateway <c>X-User-Id</c>/<c>X-Client-Id</c>/<c>X-Session-Id</c>), yet
/// their audit events must name who did it: a login's events carry the signed-in user, client and new session,
/// and a self sign-up's carry the new user — while a user created by an authenticated caller stays attributed to
/// that caller. Requires PostgreSQL + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class SignInAuditEndToEndTests : IAsyncLifetime
{
    // Matches the seeded client (Seed:ClientId / Seed:ClientSecret in appsettings).
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _loginUserName = $"audit-login-{Guid.NewGuid():N}";
    private readonly string _signUpUserName = $"audit-signup-{Guid.NewGuid():N}";
    private readonly string _adminCreatedUserName = $"audit-admin-{Guid.NewGuid():N}";
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
                var names = new[] { _loginUserName, _signUpUserName, _adminCreatedUserName };
                var ids = await context.Set<User>().IgnoreQueryFilters()
                    .Where(u => names.Contains(u.UserName)).Select(u => u.Id).ToListAsync();
                await context.Set<UserSession>().IgnoreQueryFilters()
                    .Where(s => s.UserId != null && ids.Contains(s.UserId.Value)).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters()
                    .Where(u => ids.Contains(u.Id)).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Login_events_name_the_signed_in_user_client_and_session()
    {
        if (!_infraReady) return;

        var response = await _factory.CreateClient().PostAsync("/connect/token", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = _loginUserName,
                ["password"] = Password,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
            }));
        await response.ShouldBeOkAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var session = await context.Set<UserSession>().IgnoreQueryFilters()
            .Where(s => s.UserId == _loginUserId).OrderByDescending(s => s.CreatedAt).FirstAsync();
        var created = await EventAsync(context, "GMUserSessionCreatedDomainEvent", session.Id);

        Assert.Equal(_loginUserId, created.UserId);
        Assert.Equal(Guid.Parse(ClientId), created.ClientId);
        Assert.Equal(session.Id, created.SessionId);
    }

    [Fact]
    public async Task Self_sign_up_is_attributed_to_the_new_user()
    {
        if (!_infraReady) return;

        var userId = await CreateUserAsync(_signUpUserName, callerId: null);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(userId, (await EventAsync(context, "GMUserCreatedDomainEvent", userId)).UserId);
    }

    [Fact]
    public async Task A_user_created_by_an_authenticated_caller_stays_attributed_to_that_caller()
    {
        if (!_infraReady) return;
        var callerId = Guid.NewGuid();

        var userId = await CreateUserAsync(_adminCreatedUserName, callerId);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(callerId, (await EventAsync(context, "GMUserCreatedDomainEvent", userId)).UserId);
    }

    // POST /api/v1/Users (sign-up needs no permission); callerId plays the gateway-forwarded X-User-Id.
    private async Task<Guid> CreateUserAsync(string userName, Guid? callerId)
    {
        var body = JsonSerializer.Serialize(new { username = userName, email = $"{userName}@test.local", password = Password });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Users")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        if (callerId is { } id) request.Headers.Add("X-User-Id", id.ToString("D"));

        var response = await _factory.CreateClient().SendAsync(request);
        await response.ShouldBeOkAsync();
        return Guid.Parse((await response.Content.ReadAsStringAsync()).Trim('"'));
    }

    private static Task<DomainEventLog> EventAsync(ApplicationDbContext context, string eventType, Guid aggregateId)
    {
        var id = aggregateId.ToString();
        return context.Set<DomainEventLog>()
            .Where(e => e.EventType == eventType && e.AggregateId == id)
            .OrderByDescending(e => e.CreatedAt)
            .FirstAsync();
    }
}
