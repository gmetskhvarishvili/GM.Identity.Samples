using GM.Identity.Sample.Application.Common.Authorization;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Infrastructure.Authorization;
using GM.Identity.Sample.Persistence.Context;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// Integration tests for the Redis reconciliation jobs: seed the database (the source of truth), run the
/// job, and assert the Redis projection was rebuilt from it. Exercises the RBAC and scope reconcilers
/// end-to-end against real Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class ReconciliationJobTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private bool _infraReady;

    public Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<IScopeCache>();
            _ = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
            _infraReady = true;
        }
        catch
        {
            _infraReady = false;
        }
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    [Fact]
    public async Task Scope_reconcile_rebuilds_the_client_scope_operation_projection_from_the_db()
    {
        if (!_infraReady) return;

        var operationName = $"recon:op-{Guid.NewGuid():N}";
        Guid clientId, scopeId, operationId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cache = scope.ServiceProvider.GetRequiredService<IScopeCache>();

            // ClientScope has an FK to Clients, so the client must exist.
            var client = Client.Create($"recon-client-{Guid.NewGuid():N}");
            var (secretHash, secretSalt) = PasswordHasher.Hash("client-secret");
            client.UpdateSecret(secretHash, secretSalt);
            await context.Set<Client>().AddAsync(client);

            var scopeEntity = Scope.Create($"recon-scope-{Guid.NewGuid():N}");
            var operation = Operation.Create(operationName, "reconcile test operation");
            await context.Set<Scope>().AddAsync(scopeEntity);
            await context.Set<Operation>().AddAsync(operation);
            await context.SaveChangesAsync();
            clientId = client.Id;
            scopeId = scopeEntity.Id;
            operationId = operation.Id;

            await context.Set<ScopeOperation>().AddAsync(ScopeOperation.Create(scopeId, operationId));
            await context.Set<ClientScope>().AddAsync(ClientScope.Create(clientId, scopeId));
            await context.SaveChangesAsync();

            // Cache is empty for these fresh ids; the reconcile must populate it from the DB.
            var job = ActivatorUtilities.CreateInstance<ScopeCacheReconciliationJob>(scope.ServiceProvider);
            await job.ExecuteAsync(null!, CancellationToken.None);

            Assert.Equal(operationId, await cache.GetOperationIdByNameAsync(operationName));
            Assert.True(await cache.HasOperationAsync(clientId, operationId));
        }

        await CleanupScopeAsync(clientId, scopeId, operationId);
    }

    [Fact]
    public async Task Permission_reconcile_rebuilds_the_user_role_permission_projection_from_the_db()
    {
        if (!_infraReady) return;

        Guid userId, roleId, permissionId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();

            var user = User.Create($"recon-user-{Guid.NewGuid():N}", null, null);
            var (hash, salt) = PasswordHasher.Hash("Recon123!");
            user.UpdatePassword(hash, salt);
            var role = Role.Create($"recon-role-{Guid.NewGuid():N}");
            var permission = Permission.Create($"recon:perm-{Guid.NewGuid():N}", "reconcile test permission");
            await context.Set<User>().AddAsync(user);
            await context.Set<Role>().AddAsync(role);
            await context.Set<Permission>().AddAsync(permission);
            await context.SaveChangesAsync();
            userId = user.Id;
            roleId = role.Id;
            permissionId = permission.Id;

            await context.Set<RolePermission>().AddAsync(RolePermission.Create(roleId, permissionId));
            await context.Set<UserRole>().AddAsync(UserRole.Create(userId, roleId));
            await context.SaveChangesAsync();

            var job = ActivatorUtilities.CreateInstance<PermissionCacheReconciliationJob>(scope.ServiceProvider);
            await job.ExecuteAsync(null!, CancellationToken.None);

            Assert.Contains(roleId, await cache.GetUserRoleIdsAsync(userId));
            Assert.True(await cache.HasPermissionAsync(userId, permissionId));
        }

        await CleanupPermissionAsync(userId, roleId, permissionId);
    }

    private async Task CleanupScopeAsync(Guid clientId, Guid scopeId, Guid operationId)
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cache = scope.ServiceProvider.GetRequiredService<IScopeCache>();
            await context.Set<ClientScope>().Where(x => x.ClientId == clientId).ExecuteDeleteAsync();
            await context.Set<ScopeOperation>().Where(x => x.ScopeId == scopeId).ExecuteDeleteAsync();
            await context.Set<Operation>().Where(x => x.Id == operationId).ExecuteDeleteAsync();
            await context.Set<Scope>().Where(x => x.Id == scopeId).ExecuteDeleteAsync();
            await context.Set<Client>().Where(x => x.Id == clientId).ExecuteDeleteAsync();
            await cache.RemoveClientAsync(clientId);
            await cache.RemoveScopeAsync(scopeId);
        }
        catch { /* best-effort */ }
    }

    private async Task CleanupPermissionAsync(Guid userId, Guid roleId, Guid permissionId)
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cache = scope.ServiceProvider.GetRequiredService<IPermissionCache>();
            await context.Set<UserRole>().Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await context.Set<RolePermission>().Where(x => x.RoleId == roleId).ExecuteDeleteAsync();
            await context.Set<User>().IgnoreQueryFilters().Where(x => x.Id == userId).ExecuteDeleteAsync();
            await context.Set<Role>().Where(x => x.Id == roleId).ExecuteDeleteAsync();
            await context.Set<Permission>().Where(x => x.Id == permissionId).ExecuteDeleteAsync();
            await cache.RemoveUserAsync(userId);
            await cache.RemoveRoleAsync(roleId);
        }
        catch { /* best-effort */ }
    }
}
