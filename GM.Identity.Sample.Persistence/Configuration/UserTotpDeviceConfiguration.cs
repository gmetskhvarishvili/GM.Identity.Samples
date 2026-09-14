using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTotpDeviceAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class UserTotpDeviceConfiguration : IEntityTypeConfiguration<UserTotpDevice>
{
    public void Configure(EntityTypeBuilder<UserTotpDevice> builder)
    {
        builder.ToTable("user_totp_devices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.SecretBase32).IsRequired();
        builder.Property(x => x.IsConfirmed).IsRequired();
        builder.Property(x => x.ConfirmedAt);
        builder.HasIndex(x => x.UserId);
    }
}
