using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Application.Operations.Queries.GetOperationsList;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

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
        var countSpec = new ScopeOperationSpecification(
            request.ScopeId);
        countSpec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        
        var totalCount = await unitOfWork
            .ScopeOperationRepository
            .CountAsync(
                countSpec,
                cancellationToken);
        
        var spec = new ScopeOperationSpecification(request.ScopeId, request.CurrentPage, request.PageSize,
            request.OrderBy);
        spec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        var entities = await unitOfWork.ScopeOperationRepository.ListAsync(spec, cancellationToken);

        return new PagedListDto<OperationDto>
        {
            Items = entities.Select(x=>x.Operation).Adapt<List<OperationDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}