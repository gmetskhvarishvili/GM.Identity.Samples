using GM.EntityFramework.Domain.Repositories;

namespace GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;

public interface IOutboxMessageRepository : IGenericRepository<OutboxMessage>;
