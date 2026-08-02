using GM.EntityFramework.Domain.Specifications;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate.Specifications;

public class UserSpecification : BaseSpecification<User>
{
    public UserSpecification(Guid? id, string? email, string? username)
    {
        AddVisibilityFilter();

        if (id.HasValue && id.Value != Guid.Empty)
            AddCriteria(s => s.Id == id);

        if (!string.IsNullOrWhiteSpace(email))
            AddCriteria(s => s.Email.Contains(email));

        if (!string.IsNullOrWhiteSpace(username))
            AddCriteria(s => s.UserName.Contains(username));
    }
    
    public UserSpecification(Guid? id, string? email, string? username, int currentPage, int pageSize, string orderBy)
    : this(id, email, username)
    {
        ApplyPaging(currentPage, pageSize);

        ApplyOrdering(orderBy);
    }
}