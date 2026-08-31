using GM.EntityFramework.Domain.Specifications;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate.Specifications;

public class ClientSessionSpecification : BaseSpecification<ClientSession>
{
    public ClientSessionSpecification(
        Guid? id,
        Guid? clientId,
        bool? isRevoked,
        bool? isExpired)
    {
        AddVisibilityFilter();

        if (id.HasValue && id.Value != Guid.Empty)
            AddCriteria(s => s.Id == id);

        if (clientId.HasValue && clientId.Value != Guid.Empty)
            AddCriteria(s => s.ClientId == clientId);

        if (isRevoked != null)
            AddCriteria(s => s.IsRevoked == isRevoked);

        if (isExpired == null) return;

        var datetimeNow = DateTime.UtcNow;
        if (isExpired == true)
            AddCriteria(s => s.ExpiresAt <= datetimeNow);
        else
            AddCriteria(s => s.ExpiresAt > datetimeNow);
    }

    public ClientSessionSpecification(
        Guid? id,
        Guid? clientId,
        bool? isRevoked,
        bool? isExpired,
        int currentPage,
        int pageSize,
        string orderBy)
        : this(id, clientId, isRevoked, isExpired)
    {
        ApplyPaging(currentPage, pageSize);

        ApplyOrdering(orderBy);
    }
}