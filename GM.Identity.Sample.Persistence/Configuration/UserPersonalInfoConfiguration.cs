using GM.Identity.Persistence.Configuration;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

// Column mapping comes from the GM.Identity base config; this fixes it to the sample's aggregate and table
// (default schema) and wires the 1:1 ownership relationship to User (which creates the unique FK index).
public class UserPersonalInfoConfiguration : GMUserPersonalInfoConfiguration<UserPersonalInfo>
{
    public UserPersonalInfoConfiguration() : base(null, "user_personal_info") { }

    public override void Configure(EntityTypeBuilder<UserPersonalInfo> builder)
    {
        base.Configure(builder);

        builder.HasOne<User>()
            .WithOne(u => u.PersonalInfo)
            .HasForeignKey<UserPersonalInfo>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
