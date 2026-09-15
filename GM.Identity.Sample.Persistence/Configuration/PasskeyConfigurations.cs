using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class UserPasskeyConfiguration : IEntityTypeConfiguration<UserPasskey>
{
    public void Configure(EntityTypeBuilder<UserPasskey> builder)
    {
        builder.ToTable("user_passkeys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CredentialId).IsRequired();
        builder.Property(x => x.PublicKeySpki).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.SignCount);
        builder.HasIndex(x => x.CredentialId);
        builder.HasIndex(x => x.UserId);
    }
}

public class PasskeyChallengeConfiguration : IEntityTypeConfiguration<PasskeyChallenge>
{
    public void Configure(EntityTypeBuilder<PasskeyChallenge> builder)
    {
        builder.ToTable("passkey_challenges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Challenge).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.HasIndex(x => x.UserId);
    }
}
