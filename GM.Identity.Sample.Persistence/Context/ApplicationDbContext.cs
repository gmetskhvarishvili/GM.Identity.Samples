using System.Reflection;
using GM.EntityFramework.Domain.Common;
using GM.EntityFramework.Persistence;
using GM.EntityFramework.Persistence.Infrastructure;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GM.Identity.Sample.Persistence.Context;

public class ApplicationDbContext (
    DbContextOptions<ApplicationDbContext> options, 
    IClock? clock = null) : GenericDbContext(options)
{
    public const string DefaultSchema = "application";
    private readonly IClock _clock = clock ?? new SystemClock();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        AddAuditData();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddAuditData();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void AddAuditData()
    {
        foreach (var entityEntry in ChangeTracker.Entries().Where(e =>
                 {
                     var flag = e.State switch
                     {
                         EntityState.Modified or EntityState.Added => true,
                         _ => false
                     };
                     return flag;
                 }).ToList())
        {
            var entity = entityEntry.Entity;
            var utcNow = this._clock.UtcNow;
            var properties = entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (entityEntry.State == EntityState.Added)
                SetPropertyIfExists(properties, entity, "CreatedAt", utcNow);
            SetPropertyIfExists(properties, entity, "UpdatedAt", utcNow);
        }
    }

    private static void SetPropertyIfExists(
        PropertyInfo[] props,
        object entity,
        string name,
        object? value)
    {
        props.FirstOrDefault((Func<PropertyInfo, bool>) (p => p.Name == name && p.CanWrite))?.SetValue(entity, value);
    }
}