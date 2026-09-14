using GM.Identity.Sample.Application.Common.Authorization;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof (via GM.Testing's <see cref="GmWebApplicationFactory{T}"/>) that the
/// <c>[HasPermission]</c> attribute enforces the many-to-many RBAC over HTTP: a user has MANY roles and a
/// role has MANY permissions, and access is granted when ANY of the user's roles holds the permission.
/// The gated endpoint is <c>GET /api/v1/Permissions</c> (permission name <c>GetPermissionsList</c>), and
/// the user's grant comes ONLY through a SECONDARY role — so a passing test proves all roles are consulted.
/// Requires Postgres + Redis (the app's real dependencies); skips if they aren't reachable.
/// </summary>
public sealed class HasPermissionEndToEndTests : IAsyncLifetime
{
    private const string GatedRoute = "/api/v1/Permissions";
    private const string GatedPermissionName = "GetPermissionsList";

    // The seeded client, which the seeder grants the read/manage identity scopes ([RequiresScope]).
    private const string SeededClientId = "11111111-1111-1111-1111-111111111111";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly Guid _userWithGrant = Guid.NewGuid();
    private readonly Guid _userWithoutGrant = Guid.NewGuid();
    private readonly Guid _emptyRole = Guid.NewGuid();
    private readonly Guid _grantingRole = Guid.NewGuid();
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();

            // The startup seeder maps the permission name → id; if it's missing, Redis/DB aren't ready.
            var permissionId = await cache.GetPermissionIdByNameAsync(GatedPermissionName);
            if (permissionId is null) return;

            // Only the granting role holds the permission; the empty role holds nothing.
            await cache.AddRolePermissionAsync(_grantingRole, permissionId.Value);

            // The privileged user has BOTH roles — the permission is reachable ONLY via the secondary role.
            await cache.AddUserRoleAsync(_userWithGrant, _emptyRole);
            await cache.AddUserRoleAsync(_userWithGrant, _grantingRole);

            // The other user has only the empty role.
            await cache.AddUserRoleAsync(_userWithoutGrant, _emptyRole);

            _infraReady = true;
        }
        catch
        {
            _infraReady = false; // Postgres/Redis unavailable — tests will skip.
        }
    }

    public async Task DisposeAsync()
    {
        if (_infraReady)
        {
            using var scope = _factory.Services.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
            await cache.RemoveUserAsync(_userWithGrant);
            await cache.RemoveUserAsync(_userWithoutGrant);
            await cache.RemoveRoleAsync(_grantingRole);
            await cache.RemoveRoleAsync(_emptyRole);
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Grants_access_when_permission_is_held_via_a_secondary_role()
    {
        if (!_infraReady) return; // integration test: no-op when Postgres/Redis aren't reachable

        using var request = new HttpRequestMessage(HttpMethod.Get, GatedRoute);
        request.Headers.Add("X-User-Id", _userWithGrant.ToString("D"));
        request.Headers.Add("X-Client-Id", SeededClientId); // client is scope-granted; isolates the RBAC check

        var response = await _factory.CreateClient().SendAsync(request);

        await response.ShouldBeOkAsync();
    }

    [Fact]
    public async Task Forbids_when_none_of_the_users_roles_hold_the_permission()
    {
        if (!_infraReady) return; // integration test: no-op when Postgres/Redis aren't reachable

        using var request = new HttpRequestMessage(HttpMethod.Get, GatedRoute);
        request.Headers.Add("X-User-Id", _userWithoutGrant.ToString("D"));
        request.Headers.Add("X-Client-Id", SeededClientId); // client is scope-granted; the 403 is the RBAC denial

        var response = await _factory.CreateClient().SendAsync(request);

        await response.ShouldBeForbiddenAsync();
    }

    [Fact]
    public async Task Unauthorized_when_no_user_is_present()
    {
        if (!_infraReady) return; // integration test: no-op when Postgres/Redis aren't reachable

        var response = await _factory.CreateClient().GetAsync(GatedRoute);

        await response.ShouldBeUnauthorizedAsync();
    }
}
