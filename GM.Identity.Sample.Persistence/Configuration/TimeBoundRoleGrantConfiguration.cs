using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.TimeBoundRoleGrantAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class TimeBoundRoleGrantConfiguration : IEntityTypeConfiguration<TimeBoundRoleGrant>
{
    public void Configure(EntityTypeBuilder<TimeBoundRoleGrant> builder)
    {
        builder.ToTable("time_bound_role_grants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.RoleId).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.HasIndex(x => x.UserId);
    }
}
