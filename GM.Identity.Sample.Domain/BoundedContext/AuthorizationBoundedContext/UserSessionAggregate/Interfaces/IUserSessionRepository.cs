using GM.EntityFramework.Domain.Repositories;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.Interfaces;

public interface IUserSessionRepository : IGenericRepository<UserSession>;