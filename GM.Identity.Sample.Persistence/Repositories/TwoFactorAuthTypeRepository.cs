using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.TwoFactorAuthTypeAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class TwoFactorAuthTypeRepository(ApplicationDbContext context)
    : GenericRepository<TwoFactorAuthType,
        ApplicationDbContext>(context), ITwoFactorAuthTypeRepository;