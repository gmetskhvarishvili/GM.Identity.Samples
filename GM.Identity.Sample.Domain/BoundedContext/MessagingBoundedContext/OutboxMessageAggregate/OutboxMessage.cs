using System.Text.Json;
using GM.EntityFramework.Domain.Abstractions;
using GM.Messaging.Domain.Events;
using GM.Messaging.Domain.Outbox;

namespace GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;

public class OutboxMessage : GMOutboxMessage, IAggregateRoot
{
    private OutboxMessage() { }

    private OutboxMessage(IIntegrationEvent evt, JsonSerializerOptions? options, string? orderingKey)
        : base(evt, options, orderingKey) { }

    /// <summary>
    /// The event as an outbox message: its stable name and version, payload, user (the event's <c>UserId</c>), ordering
    /// key (from <see cref="IOrderedIntegrationEvent"/>) and the current trace, so the relay continues the request's trace.
    /// </summary>
    public new static OutboxMessage Create<TEvent>(TEvent evt, JsonSerializerOptions? options = null, string? orderingKey = null)
        where TEvent : IIntegrationEvent => new(evt, options, orderingKey);
}
