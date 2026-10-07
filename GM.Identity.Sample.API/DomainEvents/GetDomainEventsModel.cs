using GM.API.Models;
using System;

namespace GM.Identity.Sample.API.DomainEvents;

/// <summary>Filters for the domain-event (audit-trail) list endpoint. All optional; combine to narrow the history.</summary>
public class GetDomainEventsModel : GetBaseListModel
{
    /// <summary>The acting user whose action history to return.</summary>
    public Guid? UserId { get; set; }

    /// <summary>The acting OAuth client/application.</summary>
    public Guid? ClientId { get; set; }

    /// <summary>The session the action was performed under.</summary>
    public Guid? SessionId { get; set; }

    /// <summary>The tenant the action was performed under.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>The caller's IP address (exact match).</summary>
    public string? IpAddress { get; set; }

    /// <summary>The originating frontend channel (e.g. <c>web</c>, <c>admin</c>).</summary>
    public string? ChannelId { get; set; }

    /// <summary>The event type name (e.g. <c>GMUserPasswordChangedDomainEvent</c>).</summary>
    public string? EventType { get; set; }

    /// <summary>Inclusive lower bound of when the event occurred (UTC).</summary>
    public DateTime? OccurredFrom { get; set; }

    /// <summary>Inclusive upper bound of when the event occurred (UTC).</summary>
    public DateTime? OccurredTo { get; set; }

    /// <summary>Inclusive lower bound of when the row was written (UTC).</summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>Inclusive upper bound of when the row was written (UTC).</summary>
    public DateTime? CreatedTo { get; set; }
}
