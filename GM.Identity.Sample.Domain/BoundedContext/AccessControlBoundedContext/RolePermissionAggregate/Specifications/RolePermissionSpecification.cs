using GM.EntityFramework.Domain.Specifications;

using System;
namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate.Specifications;

public class RolePermissionSpecification : BaseSpecification<RolePermission>
{
    public RolePermissionSpecification(Guid? roleId,
        AuditDateRange dateRange, PagingOptions paging, OrderingOptions ordering)
    {
        AddVisibilityFilter();

        if (roleId.HasValue && roleId.Value != Guid.Empty)
            AddCriteria(s => s.RoleId == roleId);

        AddCriteria(s => s.Permission.IsActive && !s.Permission.IsDeleted && !s.Permission.IsHidden);

        AddInclude(x => x.Role);
        AddInclude(x => x.Permission);

        ApplyListQuery(dateRange, paging, ordering);
    }
}
