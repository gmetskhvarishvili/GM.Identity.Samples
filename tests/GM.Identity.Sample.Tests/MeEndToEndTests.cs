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
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of the owner-scoped Me endpoints: they answer for the user the gateway forwards in X-User-Id
/// (never an id in the request), need no admin permission, and /me/permissions lists every permission the user
/// holds through their roles. Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class MeEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _plainUserName = $"me-plain-{Guid.NewGuid():N}";
    private Guid _adminId;
    private Guid _plainUserId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _adminId = await context.Set<User>().IgnoreQueryFilters()
                .Where(u => u.Email == "admin@local").Select(u => u.Id).FirstAsync();

            var plain = User.Create(_plainUserName, $"{_plainUserName}@test.local", null); // no roles
            var (hash, salt) = PasswordHasher.Hash("Correct123!");
            plain.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(plain);
            await context.SaveChangesAsync();
            _plainUserId = plain.Id;
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
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _plainUserId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task My_permissions_lists_every_permission_the_user_holds_through_roles()
    {
        if (!_infraReady) return;

        var response = await GetAsUserAsync("/api/v1/me/Permissions", _adminId);
        await response.ShouldBeOkAsync();

        var permissions = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.Contains("GetPermissionsList", permissions!);   // the seeded Administrator role holds every action
        Assert.Equal(permissions!.Count, permissions.Distinct().Count());
    }

    [Fact]
    public async Task A_user_without_roles_has_no_permissions_and_needs_none_to_ask()
    {
        if (!_infraReady) return;

        var response = await GetAsUserAsync("/api/v1/me/Permissions", _plainUserId);
        await response.ShouldBeOkAsync();
        Assert.Empty((await response.Content.ReadFromJsonAsync<List<string>>())!);
    }

    [Fact]
    public async Task Me_returns_the_forwarded_user()
    {
        if (!_infraReady) return;

        var response = await GetAsUserAsync("/api/v1/me", _plainUserId);
        await response.ShouldBeOkAsync();
        Assert.Contains(_plainUserName, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Me_returns_every_user_property_but_never_the_password_hash()
    {
        if (!_infraReady) return;

        var response = await GetAsUserAsync("/api/v1/me", _adminId);
        await response.ShouldBeOkAsync();
        var body = await response.Content.ReadAsStringAsync();

        using var json = System.Text.Json.JsonDocument.Parse(body);
        var me = json.RootElement;
        foreach (var property in new[] { "tenantId", "emailConfirmed", "phoneNumber", "phoneNumberConfirmed",
                     "lockoutEnabled", "accessFailedCount", "isBlocked", "roleIds", "twoFactorAuthTypeIds" })
            Assert.True(me.TryGetProperty(property, out _), $"missing {property}");
        Assert.NotEqual(0, me.GetProperty("roleIds").GetArrayLength()); // the seeded admin holds the Administrator role
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Without_a_forwarded_user_the_me_endpoints_answer_401()
    {
        if (!_infraReady) return;

        var response = await _factory.CreateClient().GetAsync("/api/v1/me/Permissions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> GetAsUserAsync(string path, Guid userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-User-Id", userId.ToString("D"));
        return _factory.CreateClient().SendAsync(request);
    }
}
