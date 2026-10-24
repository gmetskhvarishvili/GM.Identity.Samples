using GM.EntityFramework.Domain.Specifications;
using System;

// Filters run in SQL: EF translates ToLower() to lower(), but not the culture or StringComparison overloads.
#pragma warning disable CA1304, CA1311, CA1862

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate.Specifications;

public class UserSpecification : BaseSpecification<User>
{
    public UserSpecification(Guid? id, string? email, string? username,
        AuditDateRange dateRange, PagingOptions paging, OrderingOptions ordering,
        VisibilityScope visibility = VisibilityScope.VisibleOnly)
    {
        AddVisibilityFilter(visibility);

        // Eager-load the owned 1:1 personal info so list projections can include it.
        AddInclude(s => s.PersonalInfo!);

        if (id.HasValue && id.Value != Guid.Empty)
            AddCriteria(s => s.Id == id);

        if (!string.IsNullOrWhiteSpace(email))
            AddCriteria(s => s.Email != null && s.Email.ToLower().Contains(email.ToLower()));

        if (!string.IsNullOrWhiteSpace(username))
            AddCriteria(s => s.UserName.ToLower().Contains(username.ToLower()));

        ApplyListQuery(dateRange, paging, ordering);
    }
}
