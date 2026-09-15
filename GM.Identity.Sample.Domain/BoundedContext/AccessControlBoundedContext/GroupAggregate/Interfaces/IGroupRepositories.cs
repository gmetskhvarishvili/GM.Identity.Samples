using GM.EntityFramework.Domain.Repositories;

namespace GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.GroupAggregate.Interfaces;

public interface IGroupRepository : IGenericRepository<Group>;

public interface IGroupRoleRepository : IGenericRepository<GroupRole>;

public interface IUserGroupRepository : IGenericRepository<UserGroup>;
