using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPendingContactChangeAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class UserPendingContactChangeConfiguration : IEntityTypeConfiguration<UserPendingContactChange>
{
    public void Configure(EntityTypeBuilder<UserPendingContactChange> builder)
    {
        builder.ToTable("user_pending_contact_changes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ConfirmationType).IsRequired();
        builder.Property(x => x.NewContact).IsRequired();
        builder.HasIndex(x => x.UserId);
    }
}
