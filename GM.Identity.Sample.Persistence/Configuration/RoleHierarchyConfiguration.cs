using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleHierarchyAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class RoleHierarchyConfiguration : IEntityTypeConfiguration<RoleHierarchy>
{
    public void Configure(EntityTypeBuilder<RoleHierarchy> builder)
    {
        builder.ToTable("role_hierarchy");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoleId).IsRequired();
        builder.Property(x => x.ParentRoleId).IsRequired();
        builder.HasIndex(x => x.RoleId);
    }
}
