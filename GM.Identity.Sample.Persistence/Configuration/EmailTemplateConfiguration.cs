using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.EmailTemplateAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("email_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).IsRequired();
        builder.Property(x => x.Subject).IsRequired();
        builder.Property(x => x.Body).IsRequired();
        builder.HasIndex(x => x.Key);
    }
}
