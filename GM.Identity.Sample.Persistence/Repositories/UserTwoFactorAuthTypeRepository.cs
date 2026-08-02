using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class UserTwoFactorAuthTypeRepository(ApplicationDbContext context)
    : GenericRepository<UserTwoFactorAuthType,
        ApplicationDbContext>(context), IUserTwoFactorAuthTypeRepository;