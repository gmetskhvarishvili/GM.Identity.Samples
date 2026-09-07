using GM.EntityFramework.Domain.Specifications;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate.Specifications;

public class ClientScopeSpecification : BaseSpecification<ClientScope>
{
    public ClientScopeSpecification(Guid? userId,
        AuditDateRange dateRange, PagingOptions paging, OrderingOptions ordering)
    {
        AddVisibilityFilter();

        if (userId.HasValue && userId.Value != Guid.Empty)
            AddCriteria(s => s.ClientId == userId);

        AddCriteria(s => s.Scope.IsActive && !s.Scope.IsDeleted && !s.Scope.IsHidden);

        AddInclude(x => x.Scope);
        AddInclude(x => x.Client);

        ApplyListQuery(dateRange, paging, ordering);
    }
}
