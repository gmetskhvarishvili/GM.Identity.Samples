using GM.EntityFramework.Domain.Repositories;

namespace GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate.Interfaces;

public interface IClientSessionRepository : IGenericRepository<ClientSession>;