using GM.EntityFramework.Domain.Specifications;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.AuditBoundedContext.AuditAggregate.Specifications;

/// <summary>
/// Filters the audit log by any combination of actor/request metadata and time ranges (every argument is
/// optional), newest-first. Used by the admin action-history query.
/// </summary>
public class DomainEventLogSpecification : BaseSpecification<DomainEventLog>
{
    public DomainEventLogSpecification(
        Guid? userId,
        Guid? clientId,
        Guid? sessionId,
        Guid? tenantId,
        string? ipAddress,
        string? channelId,
        string? eventType,
        DateTime? occurredFrom,
        DateTime? occurredTo,
        DateTime? createdFrom,
        DateTime? createdTo,
        PagingOptions paging)
    {
        if (userId.HasValue && userId.Value != Guid.Empty)
            AddCriteria(x => x.UserId == userId);
        if (clientId.HasValue && clientId.Value != Guid.Empty)
            AddCriteria(x => x.ClientId == clientId);
        if (sessionId.HasValue && sessionId.Value != Guid.Empty)
            AddCriteria(x => x.SessionId == sessionId);
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            AddCriteria(x => x.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(ipAddress))
            AddCriteria(x => x.IpAddress == ipAddress);
        if (!string.IsNullOrWhiteSpace(channelId))
            AddCriteria(x => x.ChannelId == channelId);
        if (!string.IsNullOrWhiteSpace(eventType))
            AddCriteria(x => x.EventType == eventType);
        if (occurredFrom.HasValue)
            AddCriteria(x => x.OccurredOn >= occurredFrom.Value);
        if (occurredTo.HasValue)
            AddCriteria(x => x.OccurredOn <= occurredTo.Value);
        if (createdFrom.HasValue)
            AddCriteria(x => x.CreatedAt >= createdFrom.Value);
        if (createdTo.HasValue)
            AddCriteria(x => x.CreatedAt <= createdTo.Value);

        ApplyPaging(paging);
        OrderBy = "OccurredOn desc";
    }
}
