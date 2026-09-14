using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class AuthorizationCodeConfiguration : IEntityTypeConfiguration<AuthorizationCode>
{
    public void Configure(EntityTypeBuilder<AuthorizationCode> builder)
    {
        builder.ToTable("authorization_codes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClientId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CodeHash).IsRequired();
        builder.Property(x => x.RedirectUri).IsRequired();
        builder.Property(x => x.Scope);
        builder.Property(x => x.CodeChallenge).IsRequired();
        builder.Property(x => x.CodeChallengeMethod).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.ConsumedAt);

        // The token exchange looks a code up by its hash.
        builder.HasIndex(x => x.CodeHash);
    }
}
