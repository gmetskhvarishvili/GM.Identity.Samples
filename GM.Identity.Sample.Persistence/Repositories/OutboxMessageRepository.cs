using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;
using GM.Messaging.Persistence.Outbox;

namespace GM.Identity.Sample.Persistence.Repositories;

public class OutboxMessageRepository(ApplicationDbContext context)
    : GenericRepository<OutboxMessage, ApplicationDbContext>(context), IOutboxMessageRepository, IOutboxDbContext<OutboxMessage>
{
    IQueryable<OutboxMessage> IOutboxDbContext<OutboxMessage>.OutboxMessages => context.Set<OutboxMessage>();

    Task<int> IOutboxDbContext<OutboxMessage>.SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
