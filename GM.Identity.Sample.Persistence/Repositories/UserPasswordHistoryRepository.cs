using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPasswordHistoryAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPasswordHistoryAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class UserPasswordHistoryRepository(ApplicationDbContext context)
    : GenericRepository<UserPasswordHistory, ApplicationDbContext>(context), IUserPasswordHistoryRepository;
