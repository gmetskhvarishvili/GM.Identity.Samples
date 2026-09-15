using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Roles.Commands.RemoveRoleParent;
using GM.Identity.Sample.Application.Roles.Commands.SetRoleParent;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleHierarchyAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate;
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
/// End-to-end proof of role hierarchy: a child role inherits its parent's permissions (a user in the child role
/// gains the parent's permission once the parent edge is set, and loses it when removed). Requires
/// Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class RoleHierarchyEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private Guid _userId;
    private Guid _parentRoleId;
    private Guid _childRoleId;
    private Guid _permissionId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = User.Create($"hier-{Guid.NewGuid():N}", $"hier-{Guid.NewGuid():N}@test.local", null);
            var (hash, salt) = PasswordHasher.Hash("Correct123!");
            user.UpdatePassword(hash, salt);
            await context.Set<User>().AddAsync(user);

            var parent = Role.Create($"parent-{Guid.NewGuid():N}");
            var child = Role.Create($"child-{Guid.NewGuid():N}");
            var permission = Permission.Create($"perm-{Guid.NewGuid():N}", "Hierarchy test permission");
            await context.Set<Role>().AddRangeAsync(parent, child);
            await context.Set<Permission>().AddAsync(permission);
            await context.SaveChangesAsync();

            // Parent role owns the permission.
            await context.Set<RolePermission>().AddAsync(RolePermission.Create(parent.Id, permission.Id));
            await context.SaveChangesAsync();

            _userId = user.Id;
            _parentRoleId = parent.Id;
            _childRoleId = child.Id;
            _permissionId = permission.Id;

            // The user is a member of the CHILD role (seed the user→role cache mapping directly).
            var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
            await cache.AddUserRoleAsync(_userId, _childRoleId);
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
                await cache.RemoveRoleAsync(_childRoleId);
                await cache.RemoveRoleAsync(_parentRoleId);
                await context.Set<RoleHierarchy>().IgnoreQueryFilters().Where(x => x.RoleId == _childRoleId).ExecuteDeleteAsync();
                await context.Set<RolePermission>().IgnoreQueryFilters().Where(x => x.RoleId == _parentRoleId).ExecuteDeleteAsync();
                await context.Set<Permission>().IgnoreQueryFilters().Where(p => p.Id == _permissionId).ExecuteDeleteAsync();
                await context.Set<Role>().IgnoreQueryFilters().Where(r => r.Id == _parentRoleId || r.Id == _childRoleId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _userId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Child_role_inherits_parent_permissions_when_parented_and_loses_them_when_unparented()
    {
        if (!_infraReady) return;

        Assert.False(await HasPermissionAsync()); // child has no own perms and no parent yet

        await SendAsync(new SetRoleParentCommand { RoleId = _childRoleId, ParentRoleId = _parentRoleId });
        Assert.True(await HasPermissionAsync()); // inherited from parent

        await SendAsync(new RemoveRoleParentCommand { RoleId = _childRoleId });
        Assert.False(await HasPermissionAsync()); // inheritance removed
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
