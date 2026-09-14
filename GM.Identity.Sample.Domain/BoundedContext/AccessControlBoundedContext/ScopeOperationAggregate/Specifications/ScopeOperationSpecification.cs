using GM.EntityFramework.Domain.Specifications;

using System;
namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate.Specifications;

public class ScopeOperationSpecification : BaseSpecification<ScopeOperation>
{
    public ScopeOperationSpecification(Guid? scopeId,
        AuditDateRange dateRange, PagingOptions paging, OrderingOptions ordering)
    {
        AddVisibilityFilter();

        if (scopeId.HasValue && scopeId.Value != Guid.Empty)
            AddCriteria(s => s.ScopeId == scopeId);

        AddCriteria(s => s.Operation.IsActive && !s.Operation.IsDeleted && !s.Operation.IsHidden);

        AddInclude(x => x.Scope);
        AddInclude(x => x.Operation);

        ApplyListQuery(dateRange, paging, ordering);
    }
}
