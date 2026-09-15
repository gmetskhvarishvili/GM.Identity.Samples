using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Application.Users.Commands.GrantTimeBoundRole;
using GM.Identity.Sample.Application.Users.Commands.RevokeTimeBoundRole;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.TimeBoundRoleGrantAggregate;
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
/// End-to-end proof of time-bound role assignments: a role granted with a future expiry takes effect
/// immediately and can be revoked; an already-expired grant confers nothing. Requires Postgres + Redis; no-ops
/// if they aren't reachable.
/// </summary>
public sealed class TimeBoundRoleEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private Guid _userId;
    private Guid _roleId;
    private Guid _permissionId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = User.Create($"tbr-{Guid.NewGuid():N}", $"tbr-{Guid.NewGuid():N}@test.local", null);
            var (hash, salt) = PasswordHasher.Hash("Correct123!");
            user.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(user);

            var role = Role.Create($"tbr-role-{Guid.NewGuid():N}");
            var permission = Permission.Create($"tbr-perm-{Guid.NewGuid():N}", "Time-bound test permission");
            await context.Set<Role>().AddAsync(role);
            await context.Set<Permission>().AddAsync(permission);
            await context.SaveChangesAsync();

            _userId = user.Id;
            _roleId = role.Id;
            _permissionId = permission.Id;

            var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
            await cache.AddRolePermissionAsync(_roleId, _permissionId); // role grants the permission
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
                var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
                await cache.RemoveUserAsync(_userId);
                await cache.RemoveRoleAsync(_roleId);
                await context.Set<TimeBoundRoleGrant>().IgnoreQueryFilters().Where(x => x.UserId == _userId).ExecuteDeleteAsync();
                await context.Set<Permission>().IgnoreQueryFilters().Where(p => p.Id == _permissionId).ExecuteDeleteAsync();
                await context.Set<Role>().IgnoreQueryFilters().Where(r => r.Id == _roleId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task A_future_grant_is_effective_and_revocable()
    {
        if (!_infraReady) return;

        Assert.False(await HasPermissionAsync());

        await SendAsync(new GrantTimeBoundRoleCommand
        {
            UserId = _userId,
            RoleId = _roleId,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        });
        Assert.True(await HasPermissionAsync());

        await SendAsync(new RevokeTimeBoundRoleCommand { UserId = _userId, RoleId = _roleId });
        Assert.False(await HasPermissionAsync());
    }

    [Fact]
    public async Task An_expired_grant_confers_nothing()
    {
        if (!_infraReady) return;

        // Seed an already-expired grant directly, then reproject the user.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Set<TimeBoundRoleGrant>()
                .AddAsync(TimeBoundRoleGrant.Create(_userId, _roleId, DateTime.UtcNow.AddHours(-1)));
            await context.SaveChangesAsync();
        }
        using (var scope = _factory.Services.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<GM.Identity.Sample.Domain.SeedWork.IUnitOfWork>();
            var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
            await UserRoleProjection.RebuildUserAsync(uow, cache, _userId, default);
        }

        Assert.False(await HasPermissionAsync());
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
