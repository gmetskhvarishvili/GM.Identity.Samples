using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Application.Infrastructure.Services.Audit;
using GM.Mediator.Contracts;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Audit.Queries.SearchAuditTrail;

/// <summary>
/// Global audit search across every aggregate (users, clients, roles, …), newest first and paged, filtered by
/// any combination of aggregate type, acting user, event type, and occurred-on date range.
/// </summary>
public class SearchAuditTrailQuery : GetBaseListQuery, IRequest<PagedListDto<AuditTrailEntry>>
{
    public string? AggregateType { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? EventType { get; set; }
    public DateTime? OccurredFrom { get; set; }
    public DateTime? OccurredTo { get; set; }
}

public class SearchAuditTrailQueryValidator : AbstractValidator<SearchAuditTrailQuery>;

public class SearchAuditTrailQueryHandler(IAuditTrailReader auditTrailReader)
    : IRequestHandler<SearchAuditTrailQuery, PagedListDto<AuditTrailEntry>>
{
    public async Task<PagedListDto<AuditTrailEntry>> Handle(
        SearchAuditTrailQuery request, CancellationToken cancellationToken)
    {
        var page = request.CurrentPage < 1 ? 1 : request.CurrentPage;
        var size = request.PageSize < 1 ? 20 : request.PageSize;

        var (items, totalCount) = await auditTrailReader.SearchAsync(
            new AuditTrailSearch
            {
                AggregateType = request.AggregateType,
                ActorUserId = request.ActorUserId,
                EventType = request.EventType,
                OccurredFrom = request.OccurredFrom,
                OccurredTo = request.OccurredTo,
            },
            (page - 1) * size, size, cancellationToken);

        return new PagedListDto<AuditTrailEntry>
        {
            Items = new List<AuditTrailEntry>(items),
            CurrentPage = page,
            PageSize = size,
            TotalCount = totalCount,
        };
    }
}
