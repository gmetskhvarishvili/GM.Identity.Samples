using GM.EntityFramework.Domain.Specifications;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate.Specifications;

public class ScopeOperationSpecification : BaseSpecification<ScopeOperation>
{
    public ScopeOperationSpecification(Guid? scopeId)
    {
        AddVisibilityFilter();

        if (scopeId.HasValue && scopeId.Value != Guid.Empty)
            AddCriteria(s => s.ScopeId == scopeId);
        
        AddCriteria(s => s.Operation.IsActive && !s.Operation.IsDeleted && !s.Operation.IsHidden);

        AddInclude(x => x.Scope);
        AddInclude(x => x.Operation);
    }
    
    public ScopeOperationSpecification(Guid? scopeId, int currentPage, int pageSize, string orderBy)
    : this(scopeId)
    {
        ApplyPaging(currentPage, pageSize);

        ApplyOrdering(orderBy);
    }
}