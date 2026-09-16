using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.DeviceCodeAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class DeviceCodeConfiguration : IEntityTypeConfiguration<DeviceCode>
{
    public void Configure(EntityTypeBuilder<DeviceCode> builder)
    {
        builder.ToTable("device_codes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClientId).IsRequired();
        builder.Property(x => x.DeviceCodeHash).IsRequired();
        builder.Property(x => x.UserCode).IsRequired();
        builder.Property(x => x.IntervalSeconds).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.UserId);
        builder.Property(x => x.LastPolledAt);

        // The token poll looks up by device-code hash; approval looks up by the user code.
        builder.HasIndex(x => x.DeviceCodeHash);
        builder.HasIndex(x => x.UserCode);
    }
}
