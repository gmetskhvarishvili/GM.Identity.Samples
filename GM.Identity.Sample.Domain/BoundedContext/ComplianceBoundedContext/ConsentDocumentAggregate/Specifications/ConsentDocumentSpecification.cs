using GM.EntityFramework.Domain.Specifications;
using System;

namespace GM.Identity.Sample.Domain.BoundedContext.ComplianceBoundedContext.ConsentDocumentAggregate.Specifications;

public class ConsentDocumentSpecification : BaseSpecification<ConsentDocument>
{
    public ConsentDocumentSpecification(Guid? id, string? consentType, bool? isMandatory, bool? isCurrent,
        AuditDateRange dateRange, PagingOptions paging, OrderingOptions ordering,
        VisibilityScope visibility = VisibilityScope.VisibleOnly)
    {
        AddVisibilityFilter(visibility);

        if (id.HasValue && id.Value != Guid.Empty)
            AddCriteria(d => d.Id == id);

        if (!string.IsNullOrWhiteSpace(consentType))
            AddCriteria(d => d.ConsentType.Contains(consentType));

        if (isMandatory.HasValue)
            AddCriteria(d => d.IsMandatory == isMandatory);

        if (isCurrent.HasValue)
            AddCriteria(d => d.IsCurrent == isCurrent);

        ApplyListQuery(dateRange, paging, ordering);
    }
}
