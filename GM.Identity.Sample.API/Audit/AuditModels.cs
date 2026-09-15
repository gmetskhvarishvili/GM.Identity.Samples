using FluentValidation;
using GM.API.Models;

using System;

namespace GM.Identity.Sample.API.Audit;

/// <summary>Query filters for the global audit search (all optional).</summary>
public class SearchAuditTrailModel : GetBaseListModel
{
    public string? AggregateType { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? EventType { get; set; }
    public DateTime? OccurredFrom { get; set; }
    public DateTime? OccurredTo { get; set; }
}

public class SearchAuditTrailModelValidator : AbstractValidator<SearchAuditTrailModel>;

/// <summary>One recorded domain event in the audit log.</summary>
public class AuditEntryModel
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
