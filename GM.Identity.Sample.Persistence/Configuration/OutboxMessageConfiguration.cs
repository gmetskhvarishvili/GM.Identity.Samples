using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Persistence.Configuration;

namespace GM.Identity.Sample.Persistence.Configuration;

public class OutboxMessageConfiguration() : OutboxMessageConfiguration<OutboxMessage>("outbox_messages");
