using GM.EntityFramework.Domain.Specifications;

using System;
namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate.Specifications;

public class UserSpecification : BaseSpecification<User>
{
    public UserSpecification(Guid? id, string? email, string? username,
        AuditDateRange dateRange, PagingOptions paging, OrderingOptions ordering)
    {
        AddVisibilityFilter();

        if (id.HasValue && id.Value != Guid.Empty)
            AddCriteria(s => s.Id == id);

        if (!string.IsNullOrWhiteSpace(email))
            AddCriteria(s => s.Email != null && s.Email.Contains(email));

        if (!string.IsNullOrWhiteSpace(username))
            AddCriteria(s => s.UserName.Contains(username));

        ApplyListQuery(dateRange, paging, ordering);
    }
}
