using GM.Identity.Sample.Domain.BoundedContext.AuditBoundedContext.AuditAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GM.Identity.Sample.Persistence.Configuration;

/// <summary>
/// Maps <see cref="DomainEventLog"/> as a read-only, keyless projection over the domain-event log table
/// (owned/written by the persistence library's StoredDomainEvent). Mapping it as a view keeps it queryable
/// while excluding it from migrations, so there is no second, conflicting table mapping.
/// </summary>
public class DomainEventLogConfiguration : IEntityTypeConfiguration<DomainEventLog>
{
    public void Configure(EntityTypeBuilder<DomainEventLog> builder)
    {
        builder.HasNoKey();
        builder.ToView("DomainEvents", "domainEvents");
    }
}
