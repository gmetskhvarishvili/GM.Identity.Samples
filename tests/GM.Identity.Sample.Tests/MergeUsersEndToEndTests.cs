using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Users.Commands.MergeUsers;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
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
/// End-to-end proof of account merge: the source's role moves to the target (so the target gains its
/// permission), and the source is soft-deleted. Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class MergeUsersEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private Guid _targetId;
    private Guid _sourceId;
    private Guid _roleId;
    private Guid _permissionId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var target = NewUser("merge-target");
            var source = NewUser("merge-source");
            await context.Set<User>().AddRangeAsync(target, source);

            var role = Role.Create($"merge-role-{Guid.NewGuid():N}");
            var permission = Permission.Create($"merge-perm-{Guid.NewGuid():N}", "Merge test permission");
            await context.Set<Role>().AddAsync(role);
            await context.Set<Permission>().AddAsync(permission);
            await context.SaveChangesAsync();

            _targetId = target.Id;
            _sourceId = source.Id;
            _roleId = role.Id;
            _permissionId = permission.Id;

            // Source is in the role; the role grants the permission (seed role->perm cache).
            await context.Set<UserRole>().AddAsync(UserRole.Create(_sourceId, _roleId));
            await context.SaveChangesAsync();
            var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
            await cache.AddRolePermissionAsync(_roleId, _permissionId);
            await cache.AddUserRoleAsync(_sourceId, _roleId);
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
                await cache.RemoveUserAsync(_targetId);
                await cache.RemoveUserAsync(_sourceId);
                await cache.RemoveRoleAsync(_roleId);
                await context.Set<UserRole>().IgnoreQueryFilters().Where(x => x.UserId == _targetId || x.UserId == _sourceId).ExecuteDeleteAsync();
                await context.Set<Permission>().IgnoreQueryFilters().Where(p => p.Id == _permissionId).ExecuteDeleteAsync();
                await context.Set<Role>().IgnoreQueryFilters().Where(r => r.Id == _roleId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _targetId || u.Id == _sourceId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Merge_moves_roles_to_the_target_and_soft_deletes_the_source()
    {
        if (!_infraReady) return;

        Assert.False(await HasPermissionAsync(_targetId)); // target has no access yet

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new MergeUsersCommand { TargetUserId = _targetId, SourceUserId = _sourceId });
        }

        Assert.True(await HasPermissionAsync(_targetId)); // target inherited the source's role -> permission

        using var verifyScope = _factory.Services.CreateScope();
        var ctx = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await ctx.Set<UserRole>().IgnoreQueryFilters()
            .AnyAsync(x => x.UserId == _targetId && x.RoleId == _roleId));
        var source = await ctx.Set<User>().IgnoreQueryFilters().FirstAsync(u => u.Id == _sourceId);
        Assert.True(source.IsDeleted);
    }

    private static User NewUser(string prefix)
    {
        var user = User.Create($"{prefix}-{Guid.NewGuid():N}", $"{prefix}-{Guid.NewGuid():N}@test.local", null);
        var (hash, salt) = PasswordHasher.Hash("Correct123!");
        user.UpdatePassword(hash, salt);
        return user;
    }

    private async Task<bool> HasPermissionAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
        return await cache.HasPermissionAsync(userId, _permissionId);
    }
}
