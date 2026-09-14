using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserRecoveryCodeAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class UserRecoveryCodeConfiguration : IEntityTypeConfiguration<UserRecoveryCode>
{
    public void Configure(EntityTypeBuilder<UserRecoveryCode> builder)
    {
        builder.ToTable("user_recovery_codes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CodeHash).IsRequired();
        builder.Property(x => x.UsedAt);

        // Consumption looks a user's unused codes up by (UserId, CodeHash).
        builder.HasIndex(x => new { x.UserId, x.CodeHash });
    }
}
