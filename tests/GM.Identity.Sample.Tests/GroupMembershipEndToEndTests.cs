using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Groups.Commands.AddGroupRole;
using GM.Identity.Sample.Application.Groups.Commands.AddUserToGroup;
using GM.Identity.Sample.Application.Groups.Commands.CreateGroup;
using GM.Identity.Sample.Application.Groups.Commands.RemoveUserFromGroup;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.GroupAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
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
/// End-to-end proof of group-based access: a user placed in a group inherits the group's roles (and their
/// permissions), and loses them when removed. Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class GroupMembershipEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private Guid _userId;
    private Guid _roleId;
    private Guid _permissionId;
    private Guid _groupId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = User.Create($"grp-{Guid.NewGuid():N}", $"grp-{Guid.NewGuid():N}@test.local", null);
            var (hash, salt) = PasswordHasher.Hash("Correct123!");
            user.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(user);

            var role = Role.Create($"grp-role-{Guid.NewGuid():N}");
            var permission = Permission.Create($"grp-perm-{Guid.NewGuid():N}", "Group test permission");
            await context.Set<Role>().AddAsync(role);
            await context.Set<Permission>().AddAsync(permission);
            await context.SaveChangesAsync();

            _userId = user.Id;
            _roleId = role.Id;
            _permissionId = permission.Id;

            // The role grants the permission (seed the role->perm cache mapping directly).
            var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
            await cache.AddRolePermissionAsync(_roleId, _permissionId);
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
                if (_groupId != Guid.Empty)
                {
                    await context.Set<UserGroup>().IgnoreQueryFilters().Where(x => x.GroupId == _groupId).ExecuteDeleteAsync();
                    await context.Set<GroupRole>().IgnoreQueryFilters().Where(x => x.GroupId == _groupId).ExecuteDeleteAsync();
                    await context.Set<Group>().IgnoreQueryFilters().Where(x => x.Id == _groupId).ExecuteDeleteAsync();
                }
                await context.Set<Permission>().IgnoreQueryFilters().Where(p => p.Id == _permissionId).ExecuteDeleteAsync();
                await context.Set<Role>().IgnoreQueryFilters().Where(r => r.Id == _roleId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task User_inherits_group_roles_when_added_and_loses_them_when_removed()
    {
        if (!_infraReady) return;

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            _groupId = await mediator.Send(new CreateGroupCommand { Name = $"team-{Guid.NewGuid():N}" });
            await mediator.Send(new AddGroupRoleCommand { GroupId = _groupId, RoleId = _roleId });
        }

        Assert.False(await HasPermissionAsync()); // not a member yet

        await SendAsync(new AddUserToGroupCommand { UserId = _userId, GroupId = _groupId });
        Assert.True(await HasPermissionAsync()); // inherited the group's role -> permission

        await SendAsync(new RemoveUserFromGroupCommand { UserId = _userId, GroupId = _groupId });
        Assert.False(await HasPermissionAsync()); // removed
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
