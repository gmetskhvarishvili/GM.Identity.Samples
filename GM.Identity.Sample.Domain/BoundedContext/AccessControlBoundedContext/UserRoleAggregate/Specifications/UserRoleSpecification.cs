using GM.EntityFramework.Domain.Specifications;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate.Specifications;

public class UserRoleSpecification : BaseSpecification<UserRole>
{
    public UserRoleSpecification(Guid? userId)
    {
        AddVisibilityFilter();
        AddCriteria(s => s.Role.IsActive && !s.Role.IsDeleted && !s.Role.IsHidden);

        if (userId.HasValue && userId.Value != Guid.Empty)
            AddCriteria(s => s.UserId == userId);

        AddInclude(x => x.Role);
        AddInclude(x => x.User);
    }

    public UserRoleSpecification(Guid? userId, int currentPage, int pageSize, string? orderBy)
        : this(userId)
    {
        ApplyPaging(currentPage, pageSize);

        ApplyOrdering(orderBy);
    }
}