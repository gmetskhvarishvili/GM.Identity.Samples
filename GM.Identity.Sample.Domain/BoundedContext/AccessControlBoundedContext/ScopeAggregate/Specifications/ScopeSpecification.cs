using GM.EntityFramework.Domain.Specifications;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate.Specifications;

public class ScopeSpecification : BaseSpecification<Scope>
{
    public ScopeSpecification(Guid? id, string? name)
    {
        AddVisibilityFilter();

        if (id.HasValue && id.Value != Guid.Empty)
            AddCriteria(s => s.Id == id);

        if (!string.IsNullOrWhiteSpace(name))
            AddCriteria(s => s.Name.Contains(name));
    }
    
    public ScopeSpecification(Guid? id, string? name, int currentPage, int pageSize, string? orderBy)
    : this(id, name)
    {
        AddVisibilityFilter();

        if (id.HasValue && id.Value != Guid.Empty)
            AddCriteria(s => s.Id == id);

        if (!string.IsNullOrWhiteSpace(name))
            AddCriteria(s => s.Name.Contains(name));
        
        ApplyPaging(currentPage, pageSize);
        
        ApplyOrdering(orderBy);
    }
}