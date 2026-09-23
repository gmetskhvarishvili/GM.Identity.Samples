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
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.IsMandatory).IsRequired();
        builder.Property(x => x.IsCurrent).IsRequired();
        // Each (ConsentType, Version) is one immutable row. Uniqueness of a type's version and the single-current
        // invariant are maintained in the command handlers (matching the Scope.Name pattern), so soft-deleted
        // rows don't block re-publishing. The index serves the by-type / current-version lookups.
        builder.HasIndex(x => new { x.ConsentType, x.IsCurrent });
    }
}
