using FluentValidation;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

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

        var countSpec = new OperationSpecification(request.Id, request.Name, request.Description,
            dateRange, PagingOptions.None, OrderingOptions.None);
        var totalCount = await unitOfWork.OperationRepository.CountAsync(countSpec, cancellationToken);

        var spec = new OperationSpecification(request.Id, request.Name, request.Description,
            dateRange, request.ToPagingOptions(), request.ToOrderingOptions());
        var entities = await unitOfWork.OperationRepository.ListAsync(spec, cancellationToken);

        return new PagedListDto<OperationDto>
        {
            Items = entities.Adapt<List<OperationDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}

public class OperationDto : AuditableDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
