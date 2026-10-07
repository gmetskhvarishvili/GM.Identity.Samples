using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.ComplianceBoundedContext.ConsentDocumentAggregate;
using GM.Identity.Sample.Domain.BoundedContext.ComplianceBoundedContext.ConsentDocumentAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class ConsentDocumentRepository(ApplicationDbContext context)
    : GenericRepository<ConsentDocument, ApplicationDbContext>(context), IConsentDocumentRepository;
