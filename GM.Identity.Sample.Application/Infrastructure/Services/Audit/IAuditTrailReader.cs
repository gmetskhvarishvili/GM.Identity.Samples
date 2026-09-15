using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Infrastructure.Services.Audit;

/// <summary>
/// Reads the durable domain-event log (the audit trail) for a single aggregate. The event store lives in the
/// persistence layer (GM.EntityFramework); this abstraction keeps the read query out of the Application layer's
/// unit-of-work while still exposing per-aggregate history to query handlers.
/// </summary>
public interface IAuditTrailReader
{
    /// <summary>
    /// Returns a page of the audit trail for one aggregate, newest first, together with the total number of
    /// recorded events for that aggregate.
    /// </summary>
    /// <param name="aggregateType">The aggregate's CLR type name as stamped on the envelope (e.g. <c>User</c>).</param>
    /// <param name="aggregateId">The aggregate's identity as stamped on the envelope (e.g. the user id).</param>
    Task<(IReadOnlyList<AuditTrailEntry> Items, int TotalCount)> GetForAggregateAsync(
        string aggregateType, string aggregateId, int skip, int take, CancellationToken cancellationToken);

    /// <summary>
    /// Searches the whole audit log across aggregates (including clients), newest first, filtered by any
    /// combination of aggregate type, acting user, event type, and occurred-on date range.
    /// </summary>
    Task<(IReadOnlyList<AuditTrailEntry> Items, int TotalCount)> SearchAsync(
        AuditTrailSearch search, int skip, int take, CancellationToken cancellationToken);
}

/// <summary>Optional filters for a global audit search; any null filter is ignored.</summary>
public class AuditTrailSearch
{
    public string? AggregateType { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? EventType { get; set; }
    public DateTime? OccurredFrom { get; set; }
    public DateTime? OccurredTo { get; set; }
}

/// <summary>One recorded domain event, with the actor/request context captured when it was raised.</summary>
public class AuditTrailEntry
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = null!;
    public DateTime OccurredOn { get; set; }

    /// <summary>The acting user, or <c>null</c> for a system/background action.</summary>
    public Guid? UserId { get; set; }

    /// <summary>The acting OAuth client, or <c>null</c>.</summary>
    public Guid? ClientId { get; set; }

    /// <summary>The tenant the action was performed under, or <c>null</c>.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>The session the action was performed under, or <c>null</c>.</summary>
    public Guid? SessionId { get; set; }

    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }

    /// <summary>The serialized event payload (JSON).</summary>
    public string? Payload { get; set; }
}
