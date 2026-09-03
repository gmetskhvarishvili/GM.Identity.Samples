using GM.EntityFramework.Domain.Specifications;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate.Specifications;

public class RolePermissionSpecification : BaseSpecification<RolePermission>
{
    public RolePermissionSpecification(Guid? roleId)
    {
        AddVisibilityFilter();

        if (roleId.HasValue && roleId.Value != Guid.Empty)
            AddCriteria(s => s.RoleId == roleId);
        
        AddCriteria(s => s.Permission.IsActive && !s.Permission.IsDeleted && !s.Permission.IsHidden);

        AddInclude(x => x.Role);
        AddInclude(x => x.Permission);
    }
    
    public RolePermissionSpecification(Guid? roleId, int currentPage, int pageSize, string? orderBy)
    : this(roleId)
    {
        ApplyPaging(currentPage, pageSize);

        ApplyOrdering(orderBy);
    }
}