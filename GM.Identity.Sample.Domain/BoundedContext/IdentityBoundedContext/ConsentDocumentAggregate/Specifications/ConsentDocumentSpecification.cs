using GM.EntityFramework.Domain.Specifications;

using System;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ConsentDocumentAggregate.Specifications;

public class ConsentDocumentSpecification : BaseSpecification<ConsentDocument>
{
    public ConsentDocumentSpecification(Guid? id, string? consentType, bool? isMandatory, bool? isCurrent,
        AuditDateRange dateRange, PagingOptions paging, OrderingOptions ordering)
    {
        AddVisibilityFilter();

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
