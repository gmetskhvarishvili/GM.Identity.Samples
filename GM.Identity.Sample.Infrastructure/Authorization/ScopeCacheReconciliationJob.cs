using GM.Identity.Sample.Application.Common.Authorization;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Scheduling;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Authorization;

/// <summary>
/// Periodically rebuilds the Redis scope projection from the database (the source of truth), so the cache
/// self-heals from any missed write-through or manual DB change: <c>clientId → {scopeId}</c> from
/// ClientScope, <c>scopeId → {operationId}</c> from ScopeOperation, and the operation name→id map from
/// Operation. Each fire is guarded by a distributed lock (by GM.Scheduling). Runs every 10 minutes.
/// </summary>
[ScheduledJob("scope-cache-reconcile", Cron = "0 0/10 * * * ?")]
public sealed class ScopeCacheReconciliationJob(
    IUnitOfWork unitOfWork,
    IScopeCache cache) : IScheduledJob
{
    public async Task<JobExecutionResult> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        // Desired state from the DB (only live rows).
        var clientScopes = (await unitOfWork.ClientScopeRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden);
        var desiredClientScopes = clientScopes
            .GroupBy(x => x.ClientId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Guid>)g.Select(x => x.ScopeId).Distinct().ToList());

        var scopeOperations = (await unitOfWork.ScopeOperationRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden);
        var desiredScopeOperations = scopeOperations
            .GroupBy(x => x.ScopeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Guid>)g.Select(x => x.OperationId).Distinct().ToList());

        // Overwrite each desired key (atomic per key).
        foreach (var (clientId, scopeIds) in desiredClientScopes)
            await cache.ReplaceClientScopesAsync(clientId, scopeIds, cancellationToken);
        foreach (var (scopeId, operationIds) in desiredScopeOperations)
            await cache.ReplaceScopeOperationsAsync(scopeId, operationIds, cancellationToken);

        // Refresh the operation name→id map (upsert; stale names simply stop resolving to a live operation).
        // Operation is tenant-owned, so read across all tenants (this system job has no tenant context) —
        // the scope projection is id-keyed and tenant-agnostic.
        var operations = (await unitOfWork.OperationRepository
                .Query(true, null)
                .IgnoreQueryFilters()
                .ToListAsync(cancellationToken))
            .Where(x => x is { IsActive: true, IsDeleted: false, IsHidden: false } && x.Name is not null);
        foreach (var operation in operations)
            await cache.SetOperationIdAsync(operation.Name!, operation.Id, cancellationToken);

        // Drop keys that no longer have a DB counterpart (deleted clients/scopes).
        var orphanClients = 0;
        foreach (var cachedClientId in await cache.GetCachedClientIdsAsync(cancellationToken))
            if (!desiredClientScopes.ContainsKey(cachedClientId))
            {
                await cache.RemoveClientAsync(cachedClientId, cancellationToken);
                orphanClients++;
            }

        var orphanScopes = 0;
        foreach (var cachedScopeId in await cache.GetCachedScopeIdsAsync(cancellationToken))
            if (!desiredScopeOperations.ContainsKey(cachedScopeId))
            {
                await cache.RemoveScopeAsync(cachedScopeId, cancellationToken);
                orphanScopes++;
            }

        var processed = desiredClientScopes.Count + desiredScopeOperations.Count;
        return JobExecutionResult.Success(
            processed,
            $"Reconciled {desiredClientScopes.Count} clients, {desiredScopeOperations.Count} scopes; " +
            $"pruned {orphanClients} client + {orphanScopes} scope orphans.");
    }
}
