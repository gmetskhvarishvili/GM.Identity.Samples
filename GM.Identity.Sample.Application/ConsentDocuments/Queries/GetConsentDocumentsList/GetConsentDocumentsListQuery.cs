using FluentValidation;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ConsentDocumentAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.ConsentDocuments.Queries.GetConsentDocumentsList;

public class GetConsentDocumentsListQuery : GetBaseListQuery, IRequest<PagedListDto<ConsentDocumentDto>>
{
    public Guid? Id { get; set; }
    public string? ConsentType { get; set; }
    public bool? IsMandatory { get; set; }

    /// <summary>Filter to current versions only (<c>true</c>) or superseded ones (<c>false</c>); null = all.</summary>
    public bool? IsCurrent { get; set; }
}

public class GetConsentDocumentsListQueryValidator : AbstractValidator<GetConsentDocumentsListQuery>;

public class GetConsentDocumentsListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetConsentDocumentsListQuery, PagedListDto<ConsentDocumentDto>>
{
    public async Task<PagedListDto<ConsentDocumentDto>> Handle(
        GetConsentDocumentsListQuery request, CancellationToken cancellationToken)
    {
        var dateRange = request.ToAuditDateRange();

        var countSpec = new ConsentDocumentSpecification(request.Id, request.ConsentType, request.IsMandatory,
            request.IsCurrent, dateRange, PagingOptions.None, OrderingOptions.None);
        var totalCount = await unitOfWork.ConsentDocumentRepository.CountAsync(countSpec, cancellationToken);

        var spec = new ConsentDocumentSpecification(request.Id, request.ConsentType, request.IsMandatory,
            request.IsCurrent, dateRange, request.ToPagingOptions(), request.ToOrderingOptions());
        var entities = await unitOfWork.ConsentDocumentRepository.ListAsync(spec, cancellationToken);

        return new PagedListDto<ConsentDocumentDto>
        {
            Items = entities.Adapt<List<ConsentDocumentDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}

public class ConsentDocumentDto : AuditableDto
{
    public Guid Id { get; set; }
    public string? ConsentType { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Version { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsCurrent { get; set; }
}
