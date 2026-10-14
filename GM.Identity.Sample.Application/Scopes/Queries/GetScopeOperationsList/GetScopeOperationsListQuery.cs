using FluentValidation;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Application.Operations.Queries.GetOperationsList;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace GM.Identity.Sample.Application.Scopes.Queries.GetScopeOperationsList;

public class GetScopeOperationsListQuery : GetBaseListQuery, IRequest<PagedListDto<OperationDto>>
{
    public Guid ScopeId { get; set; }
}

public class GetScopeOperationsListQueryValidator : AbstractValidator<GetScopeOperationsListQuery>;

public class GetScopeOperationsListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetScopeOperationsListQuery, PagedListDto<OperationDto>>
{
    public async Task<PagedListDto<OperationDto>> Handle(GetScopeOperationsListQuery request,
        CancellationToken cancellationToken)
    {
        var dateRange = request.ToAuditDateRange();
        var paging = request.ToPagingOptions();

        var countSpec = new ScopeOperationSpecification(request.ScopeId,
            dateRange, PagingOptions.None, OrderingOptions.None, request.Visibility);
        long? totalCount = paging.IncludeTotalCount
            ? await unitOfWork.ScopeOperationRepository.CountAsync(countSpec, cancellationToken)
            : null;

        var spec = new ScopeOperationSpecification(request.ScopeId, dateRange, paging, request.ToOrderingOptions(), request.Visibility);
        var entities = await unitOfWork.ScopeOperationRepository.ListAsync(spec, cancellationToken);

        return request.ToPagedListDto(entities, entities.Select(x=>x.Operation).Adapt<List<OperationDto>>(), totalCount);
    }
}
