using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserPermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserPermissionAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class UserPermissionRepository(ApplicationDbContext context)
    : GenericRepository<UserPermission, ApplicationDbContext>(context), IUserPermissionRepository;
