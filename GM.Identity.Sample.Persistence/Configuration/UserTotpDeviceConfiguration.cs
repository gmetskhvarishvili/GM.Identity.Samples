using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTotpDeviceAggregate;
using GM.OTP.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

// The TOTP enrolment mapping lives in the GM.OTP base config; this fixes it to the sample's aggregate and
// table (default schema), then adds the sample's specifics: UserId is always set here so it's required and
// indexed (devices are looked up by user), and ConfirmedAtUtc reuses the pre-existing "ConfirmedAt" column.
public class UserTotpDeviceConfiguration() : TotpEnrolmentConfiguration<UserTotpDevice>(null, "user_totp_devices")
{
    public override void Configure(EntityTypeBuilder<UserTotpDevice> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ConfirmedAtUtc).HasColumnName("ConfirmedAt");
        builder.HasIndex(x => x.UserId);
    }
}
