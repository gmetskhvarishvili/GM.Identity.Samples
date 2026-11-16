using System;
using System.Text.Json;
using GM.EntityFramework.Domain.Abstractions;
using GM.Messaging.Domain.Events;

namespace GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;

public class OutboxMessage : GM.Messaging.Domain.Outbox.OutboxMessage, IAggregateRoot
{
    private OutboxMessage() { }

    /// <summary>
    /// The event as an outbox message, attributed to <paramref name="userId"/>: its stable name and
    /// version, payload, ordering key and the current trace (so the relay continues the request's trace).
    /// </summary>
    public static OutboxMessage From<TEvent>(Guid? userId, TEvent evt, JsonSerializerOptions? options = null)
        where TEvent : IIntegrationEvent
    {
        var message = new OutboxMessage();
        message.Populate(evt, options);
        message.UserId = userId;
        return message;
    }
}
