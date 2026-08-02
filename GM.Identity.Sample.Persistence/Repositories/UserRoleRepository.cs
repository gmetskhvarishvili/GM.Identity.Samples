using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class UserRoleRepository(ApplicationDbContext context)
    : GenericRepository<UserRole,
        ApplicationDbContext>(context), IUserRoleRepository;