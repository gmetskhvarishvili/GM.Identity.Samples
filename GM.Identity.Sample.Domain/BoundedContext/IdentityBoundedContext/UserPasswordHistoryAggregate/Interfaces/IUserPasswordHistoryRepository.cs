using GM.EntityFramework.Domain.Repositories;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPasswordHistoryAggregate.Interfaces;

public interface IUserPasswordHistoryRepository : IGenericRepository<UserPasswordHistory>;
