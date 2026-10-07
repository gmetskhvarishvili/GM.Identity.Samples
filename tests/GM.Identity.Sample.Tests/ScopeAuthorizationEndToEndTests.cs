using GM.Identity.Authorization;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof that <c>[RequiresScope]</c> enforces the client (application) dimension independently
/// of RBAC: the gated endpoint <c>GET /api/v1/Scopes</c> requires both the <c>GetScopesList</c> permission
/// (user) and a client granted the <c>read:identity</c> operation via one of its scopes (client). The
/// seeded client carries that scope; a client that doesn't is forbidden even when the user is fully
/// permitted. Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class ScopeAuthorizationEndToEndTests : IAsyncLifetime
{
    private const string GatedRoute = "/api/v1/Scopes";
    private const string GatedPermissionName = "GetScopesList";

    // The seeded OAuth client, which the seeder grants the read:identity scope.
    private const string SeededClientId = "11111111-1111-1111-1111-111111111111";

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly Guid _caller = Guid.NewGuid();
    private readonly Guid _callerRole = Guid.NewGuid();
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();

            var permissionId = await cache.GetPermissionIdByNameAsync(GatedPermissionName);
            if (permissionId is null) return;

            // Caller holds the user-side permission, so only the client (scope) dimension varies per test.
            await cache.AddRolePermissionAsync(_callerRole, permissionId.Value);
            await cache.AddUserRoleAsync(_caller, _callerRole);
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
            }
            catch { /* best-effort */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Allows_a_client_granted_the_scope()
    {
        if (!_infraReady) return;

        var response = await CallAsync(clientId: SeededClientId);
        await response.ShouldBeOkAsync();
    }

    [Fact]
    public async Task Forbids_a_client_without_the_scope_even_when_the_user_is_permitted()
    {
        if (!_infraReady) return;

        var response = await CallAsync(clientId: Guid.NewGuid().ToString("D"));
        await response.ShouldBeForbiddenAsync();
    }

    [Fact]
    public async Task Unauthorized_when_no_client_identity_is_present()
    {
        if (!_infraReady) return;

        using var request = new HttpRequestMessage(HttpMethod.Get, GatedRoute);
        request.Headers.Add("X-User-Id", _caller.ToString("D")); // user present, but no X-Client-Id

        var response = await _factory.CreateClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> CallAsync(string clientId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, GatedRoute);
        request.Headers.Add("X-User-Id", _caller.ToString("D"));
        request.Headers.Add("X-Client-Id", clientId);
        return _factory.CreateClient().SendAsync(request);
    }
}
