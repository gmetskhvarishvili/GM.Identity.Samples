using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserClientConsentAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserClientConsentAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class UserClientConsentRepository(ApplicationDbContext context)
    : GenericRepository<UserClientConsent, ApplicationDbContext>(context), IUserClientConsentRepository;
