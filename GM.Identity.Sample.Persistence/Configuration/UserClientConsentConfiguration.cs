using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserClientConsentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class UserClientConsentConfiguration : IEntityTypeConfiguration<UserClientConsent>
{
    public void Configure(EntityTypeBuilder<UserClientConsent> builder)
    {
        builder.ToTable("user_client_consents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ClientId).IsRequired();
        builder.Property(x => x.Scopes).IsRequired();
        builder.Property(x => x.GrantedAt).IsRequired();

        // The authorize endpoint looks consent up by (user, client).
        builder.HasIndex(x => new { x.UserId, x.ClientId });
    }
}
