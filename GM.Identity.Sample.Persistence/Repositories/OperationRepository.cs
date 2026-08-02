using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class OperationRepository(ApplicationDbContext context)
    : GenericRepository<Operation,
        ApplicationDbContext>(context), IOperationRepository;