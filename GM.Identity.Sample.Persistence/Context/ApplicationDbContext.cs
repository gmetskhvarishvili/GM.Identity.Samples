using GM.EntityFramework.Domain.Common;
using GM.EntityFramework.Persistence;
using GM.EntityFramework.Persistence.Extensions;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPendingContactChangeAggregate;
using GM.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Persistence.Context;

public class ApplicationDbContext : GenericDbContext
{
    public const string DefaultSchema = "application";

    private readonly ICurrentActor _currentActor;
    private readonly EncryptionKeyProvider _encryptionKeyProvider;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentActor currentActor,
        EncryptionKeyProvider encryptionKeyProvider) : base(options)
    {
        _currentActor = currentActor;
        _encryptionKeyProvider = encryptionKeyProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // PII-at-rest: encrypt the pending new email/phone (stored, never queried by value) with AES.
        modelBuilder.Entity<UserPendingContactChange>()
            .Property(x => x.NewContact)
            .HasConversion(new EncryptedStringConverter(_encryptionKeyProvider.Key));

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
}
