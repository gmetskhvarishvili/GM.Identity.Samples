using GM.EntityFramework.Domain.Common;
using GM.EntityFramework.Domain.Events;
using GM.EntityFramework.Persistence;
using GM.EntityFramework.Persistence.Extensions;
using GM.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Persistence.Context;

public class ApplicationDbContext : GenericDbContext
{
    public const string DefaultSchema = "application";

    private readonly ICurrentActor _currentActor;
    private readonly IDomainEventDispatcher _dispatcher;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentActor currentActor,
        IDomainEventDispatcher dispatcher) : base(options)
    {
        _currentActor = currentActor;
        _dispatcher = dispatcher;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Multi-tenancy: scope every tenant-owned aggregate (IHasTenant) to the ambient tenant, resolved
        // per request from the gateway-forwarded X-Tenant-Id header via ICurrentActor. The mechanism lives
        // in GM.EntityFramework.Persistence; the sample's tenant-owned roots (users, roles, permissions,
        // clients, scopes, operations) just implement IHasTenant. Join aggregates stay global — they're
        // id-referenced and the reconcile jobs read the roots with IgnoreQueryFilters().
        modelBuilder.ApplyTenantQueryFilters(_currentActor);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ChangeTracker.StampTenants(_currentActor);
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChangeTracker.StampTenants(_currentActor);
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Snapshot the raised domain events before persisting: the base save copies each to the durable
        // DomainEvents log and clears the aggregates' in-memory lists. We then dispatch to the in-process
        // handlers (e.g. the RBAC cache write-through) once the changes — event rows included — are saved.
        var domainEvents = ChangeTracker.Entries<IHasDomainEvents>()
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (domainEvents.Count > 0)
        {
            await _dispatcher.DispatchRangeAsync(domainEvents, cancellationToken);
        }

        return result;
    }
}
