using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Users.Commands.GrantUserPermission;
using GM.Identity.Sample.Application.Users.Commands.RevokeUserPermission;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserPermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of direct user permissions: a permission granted directly to a user is honored by the
/// Redis authorization cache (HasPermissionAsync) with no role involved, and revoking it removes the grant.
/// Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class DirectUserPermissionEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private Guid _userId;
    private Guid _permissionId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = User.Create($"userperm-{Guid.NewGuid():N}", $"userperm-{Guid.NewGuid():N}@test.local", null);
            var (hash, salt) = PasswordHasher.Hash("Correct123!");
            user.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(user);

            var permission = Permission.Create($"perm-{Guid.NewGuid():N}", "Direct-grant test permission");
            await context.Set<Permission>().AddAsync(permission);
            await context.SaveChangesAsync();
            _userId = user.Id;
            _permissionId = permission.Id;
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
                await context.Set<UserPermission>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<Permission>().IgnoreQueryFilters().Where(p => p.Id == _permissionId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Granting_a_permission_directly_is_honored_by_the_cache_and_revocable()
    {
        if (!_infraReady) return;

        Assert.False(await HasPermissionAsync()); // not granted yet

        await SendAsync(new GrantUserPermissionCommand { UserId = _userId, PermissionId = _permissionId });
        Assert.True(await HasPermissionAsync()); // direct grant honored, no role involved

        await SendAsync(new RevokeUserPermissionCommand { UserId = _userId, PermissionId = _permissionId });
        Assert.False(await HasPermissionAsync()); // revoked
    }

    private async Task SendAsync(IRequest request)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(request);
    }

    private async Task<bool> HasPermissionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
        return await cache.HasPermissionAsync(_userId, _permissionId);
    }
}
