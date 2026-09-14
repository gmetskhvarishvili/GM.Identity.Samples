using GM.Identity.Sample.Application.Common.Authorization;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof that the tenant global query filter isolates data: users created under tenant A are
/// invisible to a caller acting under tenant B and vice-versa. The caller's tenant is the (gateway-
/// forwarded) <c>X-Tenant-Id</c> header, which the API resolves into <c>ICurrentActor.TenantId</c>. The
/// gated list endpoint also needs the <c>GetUsersList</c> permission, seeded into the RBAC cache for the
/// throwaway caller. Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class TenantIsolationEndToEndTests : IAsyncLifetime
{
    private const string GatedRoute = "/api/v1/Users";
    private const string GatedPermissionName = "GetUsersList";

    // The seeded client, which the seeder grants the read/manage identity scopes ([RequiresScope]).
    private const string SeededClientId = "11111111-1111-1111-1111-111111111111";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _caller = Guid.NewGuid();
    private readonly Guid _callerRole = Guid.NewGuid();
    private readonly string _userA = $"tenant-a-{Guid.NewGuid():N}";
    private readonly string _userB = $"tenant-b-{Guid.NewGuid():N}";
    private Guid _userAId;
    private Guid _userBId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();

            var permissionId = await cache.GetPermissionIdByNameAsync(GatedPermissionName);
            if (permissionId is null) return; // seed/Redis not ready

            // The caller holds a role granting GetUsersList (the user dimension is satisfied for both calls).
            await cache.AddRolePermissionAsync(_callerRole, permissionId.Value);
            await cache.AddUserRoleAsync(_caller, _callerRole);

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _userAId = await CreateUserAsync(context, _userA, _tenantA);
            _userBId = await CreateUserAsync(context, _userB, _tenantB);
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
                var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
                await cache.RemoveUserAsync(_caller);
                await cache.RemoveRoleAsync(_callerRole);

                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                // Bypass the tenant filter to remove the tenant-scoped throwaway rows.
                await context.Set<User>().IgnoreQueryFilters()
                    .Where(u => u.Id == _userAId || u.Id == _userBId)
                    .ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task A_caller_only_sees_users_in_its_own_tenant()
    {
        if (!_infraReady) return;

        var asTenantA = await ListUsersAsync(_tenantA);
        Assert.Contains(_userA, asTenantA, StringComparison.Ordinal);
        Assert.DoesNotContain(_userB, asTenantA, StringComparison.Ordinal);

        var asTenantB = await ListUsersAsync(_tenantB);
        Assert.Contains(_userB, asTenantB, StringComparison.Ordinal);
        Assert.DoesNotContain(_userA, asTenantB, StringComparison.Ordinal);
    }

    private async Task<string> ListUsersAsync(Guid tenantId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, GatedRoute);
        request.Headers.Add("X-User-Id", _caller.ToString("D"));
        request.Headers.Add("X-Client-Id", SeededClientId); // scope-granted client (endpoint is [RequiresScope])
        request.Headers.Add("X-Tenant-Id", tenantId.ToString("D"));

        var response = await _factory.CreateClient().SendAsync(request);
        await response.ShouldBeOkAsync();
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<Guid> CreateUserAsync(ApplicationDbContext context, string userName, Guid tenantId)
    {
        var user = User.Create(userName, $"{userName}@test.local", null);
        user.AssignTenant(tenantId);
        await context.Set<User>().AddAsync(user);
        await context.SaveChangesAsync();
        return user.Id;
    }
}
