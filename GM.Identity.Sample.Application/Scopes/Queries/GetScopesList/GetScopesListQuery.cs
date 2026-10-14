using FluentValidation;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        var dateRange = request.ToAuditDateRange();
        var paging = request.ToPagingOptions();

        var countSpec = new ScopeSpecification(request.Id, request.Name,
            dateRange, PagingOptions.None, OrderingOptions.None, request.Visibility);
        long? totalCount = paging.IncludeTotalCount
            ? await unitOfWork.ScopeRepository.CountAsync(countSpec, cancellationToken)
            : null;

        var spec = new ScopeSpecification(request.Id, request.Name, dateRange, paging, request.ToOrderingOptions(), request.Visibility);
        var entities = await unitOfWork.ScopeRepository.ListAsync(spec, cancellationToken);

        return request.ToPagedListDto(entities, entities.Adapt<List<ScopeDto>>(), totalCount);
    }
}

public class ScopeDto : AuditableDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}
