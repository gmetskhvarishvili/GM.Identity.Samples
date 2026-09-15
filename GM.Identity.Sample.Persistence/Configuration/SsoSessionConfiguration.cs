using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class SsoSessionConfiguration : IEntityTypeConfiguration<SsoSession>
{
    public void Configure(EntityTypeBuilder<SsoSession> builder)
    {
        builder.ToTable("sso_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TokenHash).IsRequired();
        builder.Property(x => x.AuthTime).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.IpAddress);
        builder.Property(x => x.UserAgent);

        // Silent authorization looks the session up by its cookie hash; logout enumerates a user's sessions.
        builder.HasIndex(x => x.TokenHash);
        builder.HasIndex(x => x.UserId);
    }
}
