using GM.EntityFramework.Domain.Repositories;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.SsoSessionAggregate.Interfaces;

public interface ISsoSessionRepository : IGenericRepository<SsoSession>;
