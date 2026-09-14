using System;

namespace GM.Identity.Sample.Domain.SeedWork;

/// <summary>
/// Marks an aggregate as tenant-owned. The persistence layer applies a global query filter keyed on the
/// ambient <c>ICurrentActor.TenantId</c> (so a caller only ever sees its own tenant's rows) and stamps
/// <see cref="TenantId"/> on insert from the same ambient value. A <c>null</c> tenant denotes a global
/// (cross-tenant) row — e.g. the seeded administrator — visible only to a caller with no tenant context.
/// </summary>
public interface IHasTenant
{
    /// <summary>The owning tenant, or <c>null</c> for a global/cross-tenant row.</summary>
    Guid? TenantId { get; }

    /// <summary>Assigns the owning tenant. Called by the persistence layer on insert when unset.</summary>
    void AssignTenant(Guid? tenantId);
}
