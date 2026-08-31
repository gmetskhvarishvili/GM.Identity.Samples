using GM.EntityFramework.Domain.Specifications;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.Specifications;

public class UserSessionSpecification : BaseSpecification<UserSession>
{
    public UserSessionSpecification(
        Guid? id,
        Guid? userId,
        Guid? clientId,
        bool? isRevoked,
        bool? isExpired)
    {
        AddVisibilityFilter();

        if (id.HasValue && id.Value != Guid.Empty)
            AddCriteria(s => s.Id == id);

        if (userId.HasValue && userId.Value != Guid.Empty)
            AddCriteria(s => s.UserId == clientId);
        
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

    public UserSessionSpecification(
        Guid? id,
        Guid? userId,
        Guid? clientId,
        bool? isRevoked,
        bool? isExpired,
        int currentPage,
        int pageSize,
        string orderBy)
        : this(id, userId, clientId, isRevoked, isExpired)
    {
        ApplyPaging(currentPage, pageSize);

        ApplyOrdering(orderBy);
    }
}