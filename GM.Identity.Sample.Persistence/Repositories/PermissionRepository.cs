using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class PermissionRepository(ApplicationDbContext context)
    : GenericRepository<Permission,
        ApplicationDbContext>(context), IPermissionRepository;