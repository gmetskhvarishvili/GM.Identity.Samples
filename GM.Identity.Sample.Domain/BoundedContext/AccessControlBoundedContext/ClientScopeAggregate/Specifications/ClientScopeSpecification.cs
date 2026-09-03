using GM.EntityFramework.Domain.Specifications;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate.Specifications;

public class ClientScopeSpecification : BaseSpecification<ClientScope>
{
    public ClientScopeSpecification(Guid? userId)
    {
        AddVisibilityFilter();

        if (userId.HasValue && userId.Value != Guid.Empty)
            AddCriteria(s => s.ClientId == userId);
        
        AddCriteria(s => s.Scope.IsActive && !s.Scope.IsDeleted && !s.Scope.IsHidden);

        AddInclude(x => x.Scope);
        AddInclude(x => x.Client);

    }

    public ClientScopeSpecification(Guid? userId, int currentPage, int pageSize, string? orderBy)
        : this(userId)
    {
        ApplyPaging(currentPage, pageSize);

        ApplyOrdering(orderBy);
    }
}