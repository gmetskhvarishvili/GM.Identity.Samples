using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.ComplianceBoundedContext.UserConsentAggregate;
using GM.Identity.Sample.Domain.BoundedContext.ComplianceBoundedContext.UserConsentAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class UserConsentRepository(ApplicationDbContext context)
    : GenericRepository<UserConsent, ApplicationDbContext>(context), IUserConsentRepository;
