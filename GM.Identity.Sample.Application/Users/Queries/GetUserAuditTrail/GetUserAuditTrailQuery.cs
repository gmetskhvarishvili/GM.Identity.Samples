using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Application.Infrastructure.Services.Audit;
using GM.Mediator.Contracts;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Queries.GetUserAuditTrail;

/// <summary>
/// Returns the audit trail (domain-event history) for a single user, newest first and paged. Each entry names
/// the event and the actor/request context it was raised under.
/// </summary>
public class GetUserAuditTrailQuery : GetBaseListQuery, IRequest<PagedListDto<AuditTrailEntry>>
{
    /// <summary>The aggregate CLR type name the user aggregate's domain events are stamped with.</summary>
    public const string UserAggregateType = "User";

    public Guid UserId { get; set; }
}

public class GetUserAuditTrailQueryValidator : AbstractValidator<GetUserAuditTrailQuery>
{
    public GetUserAuditTrailQueryValidator() => RuleFor(x => x.UserId).NotNull().NotEmpty();
}

public class GetUserAuditTrailQueryHandler(IAuditTrailReader auditTrailReader)
    : IRequestHandler<GetUserAuditTrailQuery, PagedListDto<AuditTrailEntry>>
{
    public async Task<PagedListDto<AuditTrailEntry>> Handle(
        GetUserAuditTrailQuery request, CancellationToken cancellationToken)
    {
        // Defend against unset/invalid paging inputs — this is a raw event-log read, not the spec pipeline
        // that normalizes them for the aggregate list queries.
        var page = request.CurrentPage < 1 ? 1 : request.CurrentPage;
        var size = request.PageSize < 1 ? 20 : request.PageSize;
        var skip = (page - 1) * size;

        var (items, totalCount) = await auditTrailReader.GetForAggregateAsync(
            GetUserAuditTrailQuery.UserAggregateType, request.UserId.ToString(), skip, size, cancellationToken);

        return new PagedListDto<AuditTrailEntry>
        {
            Items = new List<AuditTrailEntry>(items),
            CurrentPage = page,
            PageSize = size,
            TotalCount = totalCount,
        };
    }
}
