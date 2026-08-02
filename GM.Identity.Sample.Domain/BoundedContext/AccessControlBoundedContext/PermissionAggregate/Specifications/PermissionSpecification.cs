using GM.EntityFramework.Domain.Specifications;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate.Specifications;

public class PermissionSpecification : BaseSpecification<Permission>
{
    public PermissionSpecification(Guid? id, string? name, string? description)
    {
        AddVisibilityFilter();

        if (id.HasValue && id.Value != Guid.Empty)
            AddCriteria(s => s.Id == id);

        if (!string.IsNullOrWhiteSpace(name))
            AddCriteria(s => s.Name.Contains(name));

        if (!string.IsNullOrWhiteSpace(description))
            AddCriteria(s => s.Description.Contains(description));
    }

    public PermissionSpecification(Guid? id, string? name, string? description, int currentPage, int pageSize,
        string orderBy)
        : this(id, name, description)
    {
        ApplyPaging(currentPage, pageSize);

        ApplyOrdering(orderBy);
    }
}