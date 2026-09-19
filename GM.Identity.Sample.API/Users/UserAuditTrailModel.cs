using System;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>One recorded domain event in a user's audit trail, with the actor/request context it occurred under.</summary>
public class UserAuditTrailModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "EventType")]
    public string EventType { get; set; } = null!;
    [Display(ResourceType = typeof(StringResource), Name = "OccurredOn")]
    public DateTime OccurredOn { get; set; }

    [Display(ResourceType = typeof(StringResource), Name = "UserId")]
    public Guid? UserId { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid? ClientId { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "TenantId")]
    public Guid? TenantId { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "SessionId")]
    public Guid? SessionId { get; set; }

    [Display(ResourceType = typeof(StringResource), Name = "IpAddress")]
    public string? IpAddress { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "CorrelationId")]
    public string? CorrelationId { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "Payload")]
    public string? Payload { get; set; }
}
