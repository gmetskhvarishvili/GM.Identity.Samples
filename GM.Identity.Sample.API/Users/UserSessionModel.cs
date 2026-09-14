using GM.Identity.Sample.API.Common;

using System;
namespace GM.Identity.Sample.API.Users;

public class UserSessionModel : AuditableModel
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ClientId { get; set; }
    public bool IsRevoked { get;  set; }
    public DateTime? RevokedAt { get;  set; }
    public DateTime ExpiresAt { get; set; }

    // Request/tracing context captured when the session was created (see UserSession : ICapturesActorContext).
    public Guid? TenantId { get; set; }
    public string? ChannelId { get; set; }
    public string? Culture { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? Source { get; set; }
}
