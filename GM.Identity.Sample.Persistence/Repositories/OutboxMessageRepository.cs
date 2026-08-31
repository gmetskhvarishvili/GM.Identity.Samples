using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;
using GM.Messaging.Persistence.Outbox;

namespace GM.Identity.Sample.Persistence.Repositories;

public class OutboxMessageRepository(ApplicationDbContext context)
    : GenericRepository<OutboxMessage, ApplicationDbContext>(context), IOutboxMessageRepository, IOutboxDbContext<OutboxMessage>
{
    // _context is the protected DbContext field on GenericRepository; reuse it rather than
    // capturing the primary-constructor parameter separately (which would trigger CS9107).
    IQueryable<OutboxMessage> IOutboxDbContext<OutboxMessage>.OutboxMessages => _context.Set<OutboxMessage>();

    Task<int> IOutboxDbContext<OutboxMessage>.SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
