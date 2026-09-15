using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ApiKeyAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ApiKeyAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class ApiKeyRepository(ApplicationDbContext context)
    : GenericRepository<ApiKey, ApplicationDbContext>(context), IApiKeyRepository;
