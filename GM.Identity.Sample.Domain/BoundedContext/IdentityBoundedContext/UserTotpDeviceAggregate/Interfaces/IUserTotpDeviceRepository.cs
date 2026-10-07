using GM.EntityFramework.Domain.Repositories;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTotpDeviceAggregate.Interfaces;

public interface IUserTotpDeviceRepository : IGenericRepository<UserTotpDevice>;
