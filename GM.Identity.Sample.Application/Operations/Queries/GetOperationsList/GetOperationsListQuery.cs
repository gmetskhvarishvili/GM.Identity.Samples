using FluentValidation;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GM.Identity.Sample.Application.Operations.Queries.GetOperationsList;

public class GetOperationsListQuery : GetBaseListQuery, IRequest<PagedListDto<OperationDto>>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class GetOperationsListQueryValidator : AbstractValidator<GetOperationsListQuery>;

public class GetOperationsListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetOperationsListQuery, PagedListDto<OperationDto>>
{
    public async Task<PagedListDto<OperationDto>> Handle(GetOperationsListQuery request, CancellationToken cancellationToken)
    {
        var dateRange = request.ToAuditDateRange();
        var paging = request.ToPagingOptions();

        var countSpec = new OperationSpecification(request.Id, request.Name, request.Description,
            dateRange, PagingOptions.None, OrderingOptions.None, request.Visibility);
        long? totalCount = paging.IncludeTotalCount
            ? await unitOfWork.OperationRepository.CountAsync(countSpec, cancellationToken)
            : null;

        var spec = new OperationSpecification(request.Id, request.Name, request.Description,
            dateRange, paging, request.ToOrderingOptions(), request.Visibility);
        var entities = await unitOfWork.OperationRepository.ListAsync(spec, cancellationToken);

        return request.ToPagedListDto(entities, entities.Adapt<List<OperationDto>>(), totalCount);
    }
}

public class OperationDto : AuditableDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
