using GM.EntityFramework.Domain.Repositories;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate.Interfaces;

public interface IPermissionRepository : IGenericRepository<Permission>;