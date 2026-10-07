using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.AuditBoundedContext.AuditAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuditBoundedContext.AuditAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class DomainEventLogRepository(ApplicationDbContext context)
    : GenericRepository<DomainEventLog, ApplicationDbContext>(context), IDomainEventLogRepository;
