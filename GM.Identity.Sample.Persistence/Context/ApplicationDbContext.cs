using GM.EntityFramework.Domain.Common;
using GM.EntityFramework.Persistence;
using GM.EntityFramework.Persistence.Extensions;
using GM.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using System;

using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Persistence.Context;

public class ApplicationDbContext : GenericDbContext, ITenantContext
{
    public const string DefaultSchema = "application";

    private readonly ICurrentActor _currentActor;

    // GenericDbContext logs the domain events aggregates raise with each save, and dispatches them to their
    // IDomainEventHandler<>s (e.g. the RBAC cache write-through) after SaveChangesAsync.
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentActor currentActor) : base(options)
    {
        _currentActor = currentActor;
    }

    /// <summary>The tenant this unit of work acts under (from the gateway-forwarded X-Tenant-Id).</summary>
    public Guid? CurrentTenantId => _currentActor.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Multi-tenancy: scope every tenant-owned aggregate (IHasTenant) to the ambient tenant, resolved
        // per request from the gateway-forwarded X-Tenant-Id header via ICurrentActor. The mechanism lives
        // in GM.EntityFramework.Persistence; the sample's tenant-owned roots (users, roles, permissions,
        // clients, scopes, operations) just implement IHasTenant. Join aggregates stay global — they're
        // id-referenced and the reconcile jobs read the roots with IgnoreQueryFilters(). The filter reads the
        // tenant through this context (CurrentTenantId) so EF re-evaluates it per query.
        modelBuilder.ApplyTenantQueryFilters(this);
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
}
