using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class ClientRepository(ApplicationDbContext context)
    : GenericRepository<Client,
        ApplicationDbContext>(context), IClientRepository;