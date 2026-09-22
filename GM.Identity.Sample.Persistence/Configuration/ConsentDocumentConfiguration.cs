using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ConsentDocumentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class ConsentDocumentConfiguration : IEntityTypeConfiguration<ConsentDocument>
{
    public void Configure(EntityTypeBuilder<ConsentDocument> builder)
    {
        builder.ToTable("consent_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ConsentType).IsRequired();
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.CurrentVersion).IsRequired();
        builder.Property(x => x.IsMandatory).IsRequired();
        // Uniqueness of an active ConsentType is enforced in the command handler (matching the Scope.Name pattern),
        // so soft-deleted rows don't block re-creating a document with the same type.
        builder.HasIndex(x => x.ConsentType);
    }
}
