using GM.EntityFramework.Domain.Repositories;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate.Interfaces;

public interface IUserRepository : IGenericRepository<User>;