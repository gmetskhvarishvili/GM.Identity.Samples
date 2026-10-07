using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Application.Roles.Commands.CreateRole;
using GM.Identity.Sample.Application.Roles.Commands.DeleteRole;
using GM.Identity.Sample.Application.Roles.Commands.RestoreRole;
using GM.Identity.Sample.Application.Roles.Queries.GetRolesList;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of the visibility-scope mechanism: a soft-removed role drops out of the default
/// (visible-only) list, is surfaced again when the query asks for <see cref="VisibilityScope.All"/>
/// (delete soft-removes it — deactivated, hidden, and marked deleted), and is brought back by the
/// restore command. Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class RoleVisibilityEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _name = "vis-" + Guid.NewGuid().ToString("N");
    private Guid _roleId;
    private bool _infraReady;

    public Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _infraReady = true;
        }
        catch
        {
            _infraReady = false;
        }
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_infraReady && _roleId != Guid.Empty)
        {
            try
            {
                using var scope = _factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Set<Role>().IgnoreQueryFilters().Where(r => r.Id == _roleId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Soft_deleted_role_is_hidden_by_default_visible_via_scope_and_restorable()
    {
        if (!_infraReady) return; // integration test: no-op when Postgres/Redis aren't reachable

        // Create a role.
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            _roleId = await mediator.Send(new CreateRoleCommand { Name = _name });
        }

        // Visible by default right after creation.
        Assert.True(await RoleIsListed(VisibilityScope.VisibleOnly));

        // Soft-delete it.
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new DeleteRoleCommand { Id = _roleId });
        }

        // Gone from the default list. DeleteRole soft-removes (deactivate + hide + mark deleted), so the
        // widest scope (All drops the active/hidden/deleted constraints) is what surfaces it again.
        Assert.False(await RoleIsListed(VisibilityScope.VisibleOnly));
        Assert.True(await RoleIsListed(VisibilityScope.All));

        // Restore it, and it is visible again by default.
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new RestoreRoleCommand { Id = _roleId });
        }

        Assert.True(await RoleIsListed(VisibilityScope.VisibleOnly));
    }

    private async Task<bool> RoleIsListed(VisibilityScope visibility)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new GetRolesListQuery { Id = _roleId, Visibility = visibility });
        return result.Items.Any(r => r.Id == _roleId);
    }
}
