using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.TimeBoundRoleGrantAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.TimeBoundRoleGrantAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class TimeBoundRoleGrantRepository(ApplicationDbContext context)
    : GenericRepository<TimeBoundRoleGrant, ApplicationDbContext>(context), ITimeBoundRoleGrantRepository;
