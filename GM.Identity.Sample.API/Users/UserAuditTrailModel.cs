using System;

namespace GM.Identity.Sample.API.Users;

/// <summary>One recorded domain event in a user's audit trail, with the actor/request context it occurred under.</summary>
public class UserAuditTrailModel
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = null!;
    public DateTime OccurredOn { get; set; }

    public Guid? UserId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? SessionId { get; set; }

    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public string? Payload { get; set; }
}
