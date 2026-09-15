using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.GroupAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.GroupAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class GroupRepository(ApplicationDbContext context)
    : GenericRepository<Group, ApplicationDbContext>(context), IGroupRepository;

public class GroupRoleRepository(ApplicationDbContext context)
    : GenericRepository<GroupRole, ApplicationDbContext>(context), IGroupRoleRepository;

public class UserGroupRepository(ApplicationDbContext context)
    : GenericRepository<UserGroup, ApplicationDbContext>(context), IUserGroupRepository;
