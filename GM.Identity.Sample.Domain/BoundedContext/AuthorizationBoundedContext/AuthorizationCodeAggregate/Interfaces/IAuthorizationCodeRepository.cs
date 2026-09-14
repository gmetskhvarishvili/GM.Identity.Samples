using GM.EntityFramework.Domain.Repositories;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate.Interfaces;

public interface IAuthorizationCodeRepository : IGenericRepository<AuthorizationCode>;
