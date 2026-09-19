using System;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>One recorded domain event in a user's audit trail, with the actor/request context it occurred under.</summary>
public class UserAuditTrailModel
{
    /// <summary>The id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    /// <summary>The event type.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "EventType")]
    public string EventType { get; set; } = null!;
    /// <summary>The occurred on.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "OccurredOn")]
    public DateTime OccurredOn { get; set; }

    /// <summary>The user id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "UserId")]
    public Guid? UserId { get; set; }
    /// <summary>The client id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid? ClientId { get; set; }
    /// <summary>The tenant id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "TenantId")]
    public Guid? TenantId { get; set; }
    /// <summary>The session id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "SessionId")]
    public Guid? SessionId { get; set; }

    /// <summary>The ip address.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IpAddress")]
    public string? IpAddress { get; set; }
    /// <summary>The correlation id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "CorrelationId")]
    public string? CorrelationId { get; set; }
    /// <summary>The payload.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Payload")]
    public string? Payload { get; set; }
}
