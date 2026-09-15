using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class UserPasskeyRepository(ApplicationDbContext context)
    : GenericRepository<UserPasskey, ApplicationDbContext>(context), IUserPasskeyRepository;

public class PasskeyChallengeRepository(ApplicationDbContext context)
    : GenericRepository<PasskeyChallenge, ApplicationDbContext>(context), IPasskeyChallengeRepository;
