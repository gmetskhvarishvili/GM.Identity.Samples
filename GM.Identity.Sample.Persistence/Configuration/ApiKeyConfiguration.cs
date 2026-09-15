using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ApiKeyAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.KeyHash).IsRequired();
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.LastUsedAt);
        builder.HasIndex(x => x.KeyHash);
        builder.HasIndex(x => x.UserId);
    }
}
