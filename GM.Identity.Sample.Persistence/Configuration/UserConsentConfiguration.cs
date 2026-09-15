using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserConsentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.ToTable("user_consents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ConsentType).IsRequired();
        builder.Property(x => x.DocumentVersion).IsRequired();
        builder.Property(x => x.AcceptedAt).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.ConsentType });
    }
}
