using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Scopes.Queries.GetScopesList;

public class GetScopesListQuery : GetBaseListQuery, IRequest<PagedListDto<ScopeDto>>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class GetScopesListQueryValidator : AbstractValidator<GetScopesListQuery>;

public class GetScopesListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetScopesListQuery, PagedListDto<ScopeDto>>
{
    public async Task<PagedListDto<ScopeDto>> Handle(GetScopesListQuery request, CancellationToken cancellationToken)
    {
        var countSpec = new ScopeSpecification(
            request.Id, 
            request.Name);
        
        var totalCount = await unitOfWork
            .ScopeRepository
            .CountAsync(
                countSpec,
                cancellationToken);
        
        var spec = new ScopeSpecification(request.Id, request.Name, request.CurrentPage, request.PageSize,
            request.OrderBy ?? string.Empty);
        var entities = await unitOfWork.ScopeRepository.ListAsync(spec, cancellationToken);

        return new PagedListDto<ScopeDto>
        {
            Items = entities.Adapt<List<ScopeDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}

public class ScopeDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}