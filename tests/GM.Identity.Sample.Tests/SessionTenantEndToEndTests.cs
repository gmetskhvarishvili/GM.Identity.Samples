using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Infrastructure.Authorization;
using GM.Identity.Sample.Persistence.Context;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// The Redis session a login writes is pinned to the user's own tenant (null for a global user), so the
/// gateway enforces the tenant from the token instead of trusting a caller-asserted <c>X-Tenant-Id</c> (that
/// enforcement is covered by GM.Identity's handler tests). Also checks the session reconciler keeps the
/// tenant when it rebuilds the store. Requires PostgreSQL + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class SessionTenantEndToEndTests : IAsyncLifetime
{
    // Matches the seeded client (Seed:ClientId / Seed:ClientSecret in appsettings).
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly Guid _tenant = Guid.NewGuid();
    private readonly string _tenantUserName = $"tenant-session-{Guid.NewGuid():N}";
    private readonly string _globalUserName = $"global-session-{Guid.NewGuid():N}";
    private Guid _tenantUserId;
    private Guid _globalUserId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _tenantUserId = await CreateUserAsync(context, _tenantUserName, _tenant);
            _globalUserId = await CreateUserAsync(context, _globalUserName, tenantId: null);
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
                    .IgnoreQueryFilters()
                    .Where(s => s.UserId == _tenantUserId || s.UserId == _globalUserId)
                    .ExecuteDeleteAsync();
                await context.Set<User>()
                    .IgnoreQueryFilters()
                    .Where(u => u.Id == _tenantUserId || u.Id == _globalUserId)
                    .ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Login_pins_the_session_to_the_users_tenant()
    {
        if (!_infraReady) return;

        var session = await ReadSessionAsync(await LoginAsync(_tenantUserName));

        Assert.Equal(_tenantUserId, session.UserId);
        Assert.Equal(_tenant, session.TenantId);
    }

    [Fact]
    public async Task Login_of_a_global_user_leaves_the_session_global()
    {
        if (!_infraReady) return;

        var session = await ReadSessionAsync(await LoginAsync(_globalUserName));

        Assert.Equal(_globalUserId, session.UserId);
        Assert.Null(session.TenantId);
    }

    [Fact]
    public async Task Reconcile_restores_a_session_with_its_tenant()
    {
        if (!_infraReady) return;

        var token = await LoginAsync(_tenantUserName);
        var redis = _factory.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        await redis.KeyDeleteAsync(SessionKey(token)); // simulate a missed write-through

        using (var scope = _factory.Services.CreateScope())
        {
            var job = ActivatorUtilities.CreateInstance<SessionCacheReconciliationJob>(scope.ServiceProvider);
            await job.ExecuteAsync(null!, CancellationToken.None);
        }

        Assert.Equal(_tenant, (await ReadSessionAsync(token)).TenantId);
    }

    private static async Task<Guid> CreateUserAsync(ApplicationDbContext context, string userName, Guid? tenantId)
    {
        var user = User.Create(userName, $"{userName}@test.local", null);
        var (hash, salt) = PasswordHasher.Hash(Password);
        user.UpdatePassword(hash, salt);
        user.AssignTenant(tenantId);
        await context.Set<User>().AddAsync(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    private async Task<string> LoginAsync(string userName)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = userName,
            ["password"] = Password,
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
        });
        var response = await _factory.CreateClient().PostAsync("/connect/token", content);
        await response.ShouldBeOkAsync();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static string SessionKey(string token) => RedisSessionCache.Prefix + TokenGenerator.Hash(token);

    private async Task<SessionInfo> ReadSessionAsync(string token)
    {
        var redis = _factory.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var json = await redis.StringGetAsync(SessionKey(token));
        Assert.False(json.IsNullOrEmpty, "the login should have cached a session");
        return JsonSerializer.Deserialize<SessionInfo>((string)json!, JsonOptions)!;
    }
}
