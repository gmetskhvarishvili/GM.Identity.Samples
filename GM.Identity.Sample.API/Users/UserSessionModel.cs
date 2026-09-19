using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

public class UserSessionModel : AuditableModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "UserId")]
    public Guid? UserId { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid? ClientId { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "IsRevoked")]
    public bool IsRevoked { get;  set; }
    [Display(ResourceType = typeof(StringResource), Name = "RevokedAt")]
    public DateTime? RevokedAt { get;  set; }
    [Display(ResourceType = typeof(StringResource), Name = "ExpiresAt")]
    public DateTime ExpiresAt { get; set; }

    // Request/tracing context captured when the session was created (see UserSession : ICapturesActorContext).
    [Display(ResourceType = typeof(StringResource), Name = "TenantId")]
    public Guid? TenantId { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "ChannelId")]
    public string? ChannelId { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "CultureCode")]
    public string? Culture { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "IpAddress")]
    public string? IpAddress { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "UserAgent")]
    public string? UserAgent { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "CorrelationId")]
    public string? CorrelationId { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "IdempotencyKey")]
    public string? IdempotencyKey { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "Source")]
    public string? Source { get; set; }
}
