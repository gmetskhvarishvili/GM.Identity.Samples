using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>User session model.</summary>
public class UserSessionModel : AuditableModel
{
    /// <summary>The id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    /// <summary>The user id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "UserId")]
    public Guid? UserId { get; set; }
    /// <summary>The client id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid? ClientId { get; set; }
    /// <summary>The is revoked.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IsRevoked")]
    public bool IsRevoked { get;  set; }
    /// <summary>The revoked at.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "RevokedAt")]
    public DateTime? RevokedAt { get;  set; }
    /// <summary>The expires at.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ExpiresAt")]
    public DateTime ExpiresAt { get; set; }

    // Request/tracing context captured when the session was created (see UserSession : ICapturesActorContext).
    /// <summary>The tenant id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "TenantId")]
    public Guid? TenantId { get; set; }
    /// <summary>The channel id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ChannelId")]
    public string? ChannelId { get; set; }
    /// <summary>The culture.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "CultureCode")]
    public string? Culture { get; set; }
    /// <summary>The ip address.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IpAddress")]
    public string? IpAddress { get; set; }
    /// <summary>The user agent.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "UserAgent")]
    public string? UserAgent { get; set; }
    /// <summary>The correlation id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "CorrelationId")]
    public string? CorrelationId { get; set; }
    /// <summary>The idempotency key.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IdempotencyKey")]
    public string? IdempotencyKey { get; set; }
    /// <summary>The source.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Source")]
    public string? Source { get; set; }
}
