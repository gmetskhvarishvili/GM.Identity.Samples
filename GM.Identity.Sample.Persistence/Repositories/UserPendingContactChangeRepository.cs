using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPendingContactChangeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPendingContactChangeAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class UserPendingContactChangeRepository(ApplicationDbContext context)
    : GenericRepository<UserPendingContactChange, ApplicationDbContext>(context), IUserPendingContactChangeRepository;
