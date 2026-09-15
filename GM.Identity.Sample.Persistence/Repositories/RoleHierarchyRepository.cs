using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleHierarchyAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleHierarchyAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class RoleHierarchyRepository(ApplicationDbContext context)
    : GenericRepository<RoleHierarchy, ApplicationDbContext>(context), IRoleHierarchyRepository;
