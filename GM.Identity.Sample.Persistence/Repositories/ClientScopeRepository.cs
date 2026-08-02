using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class ClientScopeRepository(ApplicationDbContext context)
    : GenericRepository<ClientScope,
        ApplicationDbContext>(context), IClientScopeRepository;