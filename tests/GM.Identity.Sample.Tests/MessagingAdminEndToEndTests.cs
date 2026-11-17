using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end tests for GM.Messaging's outbox admin endpoints (<c>MapGMMessagingAdmin</c>) on the API: they need a
/// bearer token this API issued (401 without one) whose user holds the <c>MessagingAdmin</c> permission through a role
/// (403 without it). Requires PostgreSQL and Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class MessagingAdminEndToEndTests : IAsyncLifetime
{
    private const string Permission = "MessagingAdmin";
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";
    private static readonly Uri Outbox = new("/admin/messaging/outbox", UriKind.Relative);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _userName = $"messaging-admin-{Guid.NewGuid():N}";
    private readonly Guid _role = Guid.NewGuid();
    private Guid _userId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            // The startup seeder maps the permission name to its id; if it's missing, Redis/DB aren't ready.
            if (await scope.ServiceProvider.GetRequiredService<IPermissionCache>().GetPermissionIdByNameAsync(Permission) is null)
                return;

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
                var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
                await cache.RemoveUserAsync(_userId);
                await cache.RemoveRoleAsync(_role);
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Set<UserSession>().Where(s => s.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task OutboxBacklog_NeedsATokenWhoseUserHoldsThePermission()
    {
        if (!_infraReady) return;
        using var client = _factory.CreateClient();

        using var anonymous = await client.GetAsync(Outbox);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        using var unknownToken = await GetOutboxAsync(client, "not-a-real-token");
        Assert.Equal(HttpStatusCode.Unauthorized, unknownToken.StatusCode);

        var token = await SignInAsync(client);
        using var withoutPermission = await GetOutboxAsync(client, token);
        Assert.Equal(HttpStatusCode.Forbidden, withoutPermission.StatusCode);

        // Grant the permission through a role, as the Administrator role holds it.
        using (var scope = _factory.Services.CreateScope())
        {
            var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
            await cache.AddRolePermissionAsync(_role, (await cache.GetPermissionIdByNameAsync(Permission))!.Value);
            await cache.AddUserRoleAsync(_userId, _role);
        }

        using var response = await GetOutboxAsync(client, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"pending\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private static async Task<HttpResponseMessage> GetOutboxAsync(System.Net.Http.HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Outbox);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private async Task<string> SignInAsync(System.Net.Http.HttpClient client)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = _userName,
            ["password"] = Password,
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
        });
        using var response = await client.PostAsync(new Uri("/connect/token", UriKind.Relative), content);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }
}
