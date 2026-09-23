using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Infrastructure.Services.Audit;

/// <summary>
/// Derives facts from the durable domain-event log. The full event history isn't exposed through the API — it
/// lives in the domain-event store table and is read there directly; this abstraction only serves the internal
/// lookups the application itself needs (e.g. the last password change, for password-expiry).
/// </summary>
public interface IAuditTrailReader
{
    /// <summary>
    /// Returns when the newest event of <paramref name="eventType"/> was recorded for one aggregate, or
    /// <c>null</c> if there is none. Used to derive facts from the event log — e.g. the last password change.
    /// </summary>
    Task<DateTime?> GetLatestEventOccurredOnAsync(
        string aggregateType, string aggregateId, string eventType, CancellationToken cancellationToken);
}
