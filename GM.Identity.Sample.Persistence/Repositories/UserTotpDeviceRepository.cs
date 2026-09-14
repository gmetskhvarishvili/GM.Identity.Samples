using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTotpDeviceAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTotpDeviceAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class UserTotpDeviceRepository(ApplicationDbContext context)
    : GenericRepository<UserTotpDevice, ApplicationDbContext>(context), IUserTotpDeviceRepository;
