using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserRecoveryCodeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserRecoveryCodeAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class UserRecoveryCodeRepository(ApplicationDbContext context)
    : GenericRepository<UserRecoveryCode, ApplicationDbContext>(context), IUserRecoveryCodeRepository;
