using GM.EntityFramework.Domain.Specifications;

using System;
namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate.Specifications;

public class RoleSpecification : BaseSpecification<Role>
{
    public RoleSpecification(Guid? id, string? name,
        AuditDateRange dateRange, PagingOptions paging, OrderingOptions ordering)
    {
        AddVisibilityFilter();

        if (id.HasValue && id.Value != Guid.Empty)
            AddCriteria(s => s.Id == id);

        if (!string.IsNullOrWhiteSpace(name))
            AddCriteria(s => s.Name.Contains(name));

        ApplyListQuery(dateRange, paging, ordering);
    }
}
