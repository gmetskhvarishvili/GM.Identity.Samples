using GM.EntityFramework.Domain.Common;
using GM.EntityFramework.Persistence;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Persistence.Context;

public class ApplicationDbContext : GenericDbContext
{
    public const string DefaultSchema = "application";

    private readonly ICurrentActor _currentActor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentActor currentActor) : base(options)
    {
        _currentActor = currentActor;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Multi-tenancy enforcement: tenant-owned aggregates are automatically scoped to the ambient
        // tenant (resolved per request from the gateway-forwarded X-Tenant-Id header via ICurrentActor).
        // EF Core's default null-semantics make this correct even when the tenant is null — a null-tenant
        // caller (e.g. the seeded admin, or a system/background operation) sees only global (null-tenant)
        // rows. The filter references the injected singleton actor, so it reads the current tenant at
        // query time. See IHasTenant.
        modelBuilder.Entity<User>().HasQueryFilter(u => u.TenantId == _currentActor.TenantId);
        modelBuilder.Entity<User>().HasIndex(u => u.TenantId);

        // The identity/access-control config roots are tenant-owned too: each tenant manages its own
        // users, roles, permissions, clients, scopes and operations. The join aggregates (UserRole,
        // RolePermission, ClientScope, ScopeOperation) stay global — they're id-referenced and the RBAC /
        // scope reconcile jobs project them by (globally-unique) id, reading the tenant-owned roots with
        // IgnoreQueryFilters().
        modelBuilder.Entity<Role>().HasQueryFilter(r => r.TenantId == _currentActor.TenantId);
        modelBuilder.Entity<Role>().HasIndex(r => r.TenantId);
        modelBuilder.Entity<Permission>().HasQueryFilter(p => p.TenantId == _currentActor.TenantId);
        modelBuilder.Entity<Permission>().HasIndex(p => p.TenantId);
        modelBuilder.Entity<Client>().HasQueryFilter(c => c.TenantId == _currentActor.TenantId);
        modelBuilder.Entity<Client>().HasIndex(c => c.TenantId);
        modelBuilder.Entity<Scope>().HasQueryFilter(s => s.TenantId == _currentActor.TenantId);
        modelBuilder.Entity<Scope>().HasIndex(s => s.TenantId);
        modelBuilder.Entity<Operation>().HasQueryFilter(o => o.TenantId == _currentActor.TenantId);
        modelBuilder.Entity<Operation>().HasIndex(o => o.TenantId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Stamp the acting tenant onto newly-inserted tenant-owned rows that don't already carry one, so a
    // write lands in the caller's tenant without every handler having to remember to set it.
    private void StampTenant()
    {
        var tenantId = _currentActor.TenantId;
        foreach (var entry in ChangeTracker.Entries<IHasTenant>())
            if (entry.State == EntityState.Added && entry.Entity.TenantId is null)
                entry.Entity.AssignTenant(tenantId);
    }
}
