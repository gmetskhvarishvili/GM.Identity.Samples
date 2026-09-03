using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Application.Scopes.Queries.GetScopesList;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Clients.Queries.GetClientScopesList;

public class GetClientScopesListQuery : GetBaseListQuery, IRequest<PagedListDto<ScopeDto>>
{
    public Guid ClientId { get; set; }
}

public class GetClientScopesListQueryValidator : AbstractValidator<GetClientScopesListQuery>
{
    public GetClientScopesListQueryValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
    }
}

public class GetClientScopesListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetClientScopesListQuery, PagedListDto<ScopeDto>>
{
    public async Task<PagedListDto<ScopeDto>> Handle(GetClientScopesListQuery request, CancellationToken cancellationToken)
    {
        var countSpec = new ClientScopeSpecification(
            request.ClientId);
        countSpec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        
        var totalCount = await unitOfWork
            .ClientScopeRepository
            .CountAsync(
                countSpec,
                cancellationToken);
        
        var spec = new ClientScopeSpecification(request.ClientId, request.CurrentPage, request.PageSize,
            request.OrderBy);
        spec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        var entities = await unitOfWork.ClientScopeRepository.ListAsync(spec, cancellationToken);

        return new PagedListDto<ScopeDto>
        {
            Items = entities.Select(x=>x.Scope).Adapt<List<ScopeDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}