using System;

namespace GM.Identity.Sample.API.DomainEvents;

/// <summary>A domain-event (audit-trail) record returned to admins, including the actor/request metadata.</summary>
public class DomainEventModel
{
    public Guid Id { get; set; }
    public string? AggregateType { get; set; }
    public string? AggregateId { get; set; }
    public string? EventType { get; set; }
    public string? Payload { get; set; }
    public string? OccurredOn { get; set; }
    public string? CreatedAt { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? SessionId { get; set; }
    public string? ChannelId { get; set; }
    public string? Culture { get; set; }
    public string? Source { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public string? UserAgent { get; set; }
    public string? IdempotencyKey { get; set; }
}
