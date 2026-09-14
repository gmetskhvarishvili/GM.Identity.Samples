using GM.EntityFramework.Domain.Common;
using GM.EntityFramework.Persistence.Infrastructure;
using GM.Identity.Sample.Application.Common.Authorization;
using GM.Identity.Sample.Persistence.Context;
using GM.Identity.Sample.Persistence.Repositories;
using GM.Identity.Sample.Persistence.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests.Support;

/// <summary>
/// A self-contained, infrastructure-free host for command-handler unit tests: a real
/// <see cref="ApplicationDbContext"/> over in-memory SQLite (schema built with EnsureCreated), a real
/// <see cref="UnitOfWork"/> wired to real repositories, and in-memory cache fakes. No Postgres or Redis —
/// so these tests exercise handler logic (validation, duplicate checks, write-through) anywhere.
/// </summary>
internal sealed class InMemoryHandlerHost : IDisposable
{
    private readonly SqliteConnection _connection;

    public ApplicationDbContext Context { get; }
    public UnitOfWork Uow { get; }
    public FakeScopeCache ScopeCache { get; } = new();
    public FakePermissionCache PermissionCache { get; } = new();

    public InMemoryHandlerHost()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var actor = new FakeCurrentActor();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .ConfigureWarnings(w =>
                w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            // Stamp the auditable columns (CreatedAt/UpdatedAt/...) the same way AddPersistence does in the
            // real app, so inserts satisfy the NOT NULL audit columns.
            .AddInterceptors(new ActorAuditInterceptor(actor, new FakeClock(), NullEventSource.Instance))
            .Options;

        Context = new ApplicationDbContext(options, actor);
        Context.Database.EnsureCreated();

        Uow = new UnitOfWork(
            Context,
            new OutboxMessageRepository(Context),
            new ClientRepository(Context),
            new ClientSessionRepository(Context),
            new ClientScopeRepository(Context),
            new ScopeRepository(Context),
            new ScopeOperationRepository(Context),
            new OperationRepository(Context),
            new UserSessionRepository(Context),
            new UserRepository(Context),
            new UserRoleRepository(Context),
            new UserTwoFactorAuthTypeRepository(Context),
            new TwoFactorAuthTypeRepository(Context),
            new RoleRepository(Context),
            new RolePermissionRepository(Context),
            new PermissionRepository(Context));
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

/// <summary>A clock reading the real UTC time — enough for audit-column stamping in tests.</summary>
internal sealed class FakeClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

/// <summary>A no-op ambient actor (no user/tenant) — enough to build the model and its tenant filter.</summary>
internal sealed class FakeCurrentActor : ICurrentActor
{
    public Guid? UserId => null;
    public Guid? ClientId => null;
    public Guid? TenantId => null;
    public Guid? SessionId => null;
    public string? ChannelId => null;
    public string? Culture => null;
    public string? IpAddress => null;
    public string? CorrelationId => null;
    public string? UserAgent => null;
    public string? IdempotencyKey => null;
}

/// <summary>In-memory <see cref="IScopeCache"/> mirroring the Redis SET semantics for assertions.</summary>
internal sealed class FakeScopeCache : IScopeCache
{
    private readonly Dictionary<Guid, HashSet<Guid>> _clientScopes = new();
    private readonly Dictionary<Guid, HashSet<Guid>> _scopeOperations = new();
    private readonly Dictionary<string, Guid> _operationNames = new(StringComparer.Ordinal);

    public Task AddClientScopeAsync(Guid clientId, Guid scopeId, CancellationToken cancellationToken = default)
    {
        Set(_clientScopes, clientId).Add(scopeId);
        return Task.CompletedTask;
    }

    public Task RemoveClientScopeAsync(Guid clientId, Guid scopeId, CancellationToken cancellationToken = default)
    {
        if (_clientScopes.TryGetValue(clientId, out var set)) set.Remove(scopeId);
        return Task.CompletedTask;
    }

    public Task ReplaceClientScopesAsync(Guid clientId, IReadOnlyCollection<Guid> scopeIds, CancellationToken cancellationToken = default)
    {
        _clientScopes[clientId] = [.. scopeIds];
        return Task.CompletedTask;
    }

    public Task RemoveClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        _clientScopes.Remove(clientId);
        return Task.CompletedTask;
    }

    public Task AddScopeOperationAsync(Guid scopeId, Guid operationId, CancellationToken cancellationToken = default)
    {
        Set(_scopeOperations, scopeId).Add(operationId);
        return Task.CompletedTask;
    }

    public Task RemoveScopeOperationAsync(Guid scopeId, Guid operationId, CancellationToken cancellationToken = default)
    {
        if (_scopeOperations.TryGetValue(scopeId, out var set)) set.Remove(operationId);
        return Task.CompletedTask;
    }

    public Task ReplaceScopeOperationsAsync(Guid scopeId, IReadOnlyCollection<Guid> operationIds, CancellationToken cancellationToken = default)
    {
        _scopeOperations[scopeId] = [.. operationIds];
        return Task.CompletedTask;
    }

    public Task RemoveScopeAsync(Guid scopeId, CancellationToken cancellationToken = default)
    {
        _scopeOperations.Remove(scopeId);
        return Task.CompletedTask;
    }

    public Task SetOperationIdAsync(string operationName, Guid operationId, CancellationToken cancellationToken = default)
    {
        _operationNames[operationName] = operationId;
        return Task.CompletedTask;
    }

    public Task<Guid?> GetOperationIdByNameAsync(string operationName, CancellationToken cancellationToken = default) =>
        Task.FromResult(_operationNames.TryGetValue(operationName, out var id) ? id : (Guid?)null);

    public Task<bool> HasOperationAsync(Guid clientId, Guid operationId, CancellationToken cancellationToken = default)
    {
        var granted = _clientScopes.TryGetValue(clientId, out var scopes)
                      && scopes.Any(s => _scopeOperations.TryGetValue(s, out var ops) && ops.Contains(operationId));
        return Task.FromResult(granted);
    }

    public Task<IReadOnlyCollection<Guid>> GetCachedClientIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Guid>>([.. _clientScopes.Keys]);

    public Task<IReadOnlyCollection<Guid>> GetCachedScopeIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Guid>>([.. _scopeOperations.Keys]);

    public IReadOnlyCollection<Guid> ClientScopes(Guid clientId) =>
        _clientScopes.TryGetValue(clientId, out var set) ? [.. set] : [];

    private static HashSet<Guid> Set(Dictionary<Guid, HashSet<Guid>> map, Guid key) =>
        map.TryGetValue(key, out var set) ? set : map[key] = [];
}

/// <summary>In-memory <see cref="IPermissionCache"/> for handler write-through assertions.</summary>
internal sealed class FakePermissionCache : IPermissionCache
{
    private readonly Dictionary<Guid, HashSet<Guid>> _userRoles = new();
    private readonly Dictionary<Guid, HashSet<Guid>> _rolePermissions = new();
    private readonly Dictionary<string, Guid> _permissionNames = new(StringComparer.Ordinal);

    public Task AddUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        Set(_userRoles, userId).Add(roleId);
        return Task.CompletedTask;
    }

    public Task RemoveUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        if (_userRoles.TryGetValue(userId, out var set)) set.Remove(roleId);
        return Task.CompletedTask;
    }

    public Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        _userRoles[userId] = [.. roleIds];
        return Task.CompletedTask;
    }

    public Task RemoveUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _userRoles.Remove(userId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Guid>> GetUserRoleIdsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Guid>>(_userRoles.TryGetValue(userId, out var set) ? [.. set] : []);

    public Task AddRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        Set(_rolePermissions, roleId).Add(permissionId);
        return Task.CompletedTask;
    }

    public Task RemoveRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        if (_rolePermissions.TryGetValue(roleId, out var set)) set.Remove(permissionId);
        return Task.CompletedTask;
    }

    public Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        _rolePermissions[roleId] = [.. permissionIds];
        return Task.CompletedTask;
    }

    public Task RemoveRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        _rolePermissions.Remove(roleId);
        return Task.CompletedTask;
    }

    public Task<bool> HasPermissionAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var granted = _userRoles.TryGetValue(userId, out var roles)
                      && roles.Any(r => _rolePermissions.TryGetValue(r, out var perms) && perms.Contains(permissionId));
        return Task.FromResult(granted);
    }

    public Task SetPermissionIdAsync(string permissionName, Guid permissionId, CancellationToken cancellationToken = default)
    {
        _permissionNames[permissionName] = permissionId;
        return Task.CompletedTask;
    }

    public Task<Guid?> GetPermissionIdByNameAsync(string permissionName, CancellationToken cancellationToken = default) =>
        Task.FromResult(_permissionNames.TryGetValue(permissionName, out var id) ? id : (Guid?)null);

    public Task<IReadOnlyCollection<Guid>> GetCachedUserIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Guid>>([.. _userRoles.Keys]);

    public Task<IReadOnlyCollection<Guid>> GetCachedRoleIdsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Guid>>([.. _rolePermissions.Keys]);

    private static HashSet<Guid> Set(Dictionary<Guid, HashSet<Guid>> map, Guid key) =>
        map.TryGetValue(key, out var set) ? set : map[key] = [];
}
