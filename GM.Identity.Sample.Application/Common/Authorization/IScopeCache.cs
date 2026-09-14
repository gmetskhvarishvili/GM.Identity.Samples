using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Common.Authorization;

/// <summary>
/// A Redis-backed projection of the OAuth scope graph, kept for fast per-request scope checks:
/// <c>clientId → {scopeId}</c> (the scopes granted to a client) and <c>scopeId → {operationId}</c> (the
/// operations a scope covers), plus an <c>operationName → id</c> map so endpoints can require an operation
/// by name. The database is authoritative; this cache is updated write-through by the client-scope /
/// scope-operation / operation command handlers and reconciled periodically by
/// <c>ScopeCacheReconciliationJob</c>.
///
/// A token is authorized for an operation when <b>any</b> scope granted to its client covers that
/// operation — this enforces the modeled Client → Scope → Operation chain. It is orthogonal to RBAC
/// (<see cref="IPermissionCache"/>): scopes constrain what the calling application may do, permissions
/// constrain what the acting user may do.
/// </summary>
public interface IScopeCache
{
    // ---- client → scopes ----
    Task AddClientScopeAsync(Guid clientId, Guid scopeId, CancellationToken cancellationToken = default);
    Task RemoveClientScopeAsync(Guid clientId, Guid scopeId, CancellationToken cancellationToken = default);

    /// <summary>Overwrites a client's scope set atomically (used by reconciliation).</summary>
    Task ReplaceClientScopesAsync(Guid clientId, IReadOnlyCollection<Guid> scopeIds, CancellationToken cancellationToken = default);

    /// <summary>Drops the client's scope set entirely (e.g. the client was deleted).</summary>
    Task RemoveClientAsync(Guid clientId, CancellationToken cancellationToken = default);

    // ---- scope → operations ----
    Task AddScopeOperationAsync(Guid scopeId, Guid operationId, CancellationToken cancellationToken = default);
    Task RemoveScopeOperationAsync(Guid scopeId, Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>Overwrites a scope's operation set atomically (used by reconciliation).</summary>
    Task ReplaceScopeOperationsAsync(Guid scopeId, IReadOnlyCollection<Guid> operationIds, CancellationToken cancellationToken = default);

    /// <summary>Drops the scope's operation set entirely (e.g. the scope was deleted).</summary>
    Task RemoveScopeAsync(Guid scopeId, CancellationToken cancellationToken = default);

    // ---- operation name → id resolution (endpoints require an operation by name) ----
    Task SetOperationIdAsync(string operationName, Guid operationId, CancellationToken cancellationToken = default);
    Task<Guid?> GetOperationIdByNameAsync(string operationName, CancellationToken cancellationToken = default);

    // ---- authorization check ----
    /// <summary>True if any scope granted to <paramref name="clientId"/> covers <paramref name="operationId"/>.</summary>
    Task<bool> HasOperationAsync(Guid clientId, Guid operationId, CancellationToken cancellationToken = default);

    // ---- reconciliation support ----
    /// <summary>All client ids that currently have a cached scope set (for orphan detection).</summary>
    Task<IReadOnlyCollection<Guid>> GetCachedClientIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>All scope ids that currently have a cached operation set (for orphan detection).</summary>
    Task<IReadOnlyCollection<Guid>> GetCachedScopeIdsAsync(CancellationToken cancellationToken = default);
}
