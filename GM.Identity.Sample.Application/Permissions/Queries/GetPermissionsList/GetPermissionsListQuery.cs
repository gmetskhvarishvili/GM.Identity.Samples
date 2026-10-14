using FluentValidation;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GM.Identity.Sample.Application.Permissions.Queries.GetPermissionsList;

public class GetPermissionsListQuery : GetBaseListQuery, IRequest<PagedListDto<PermissionDto>>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class GetPermissionsListQueryValidator : AbstractValidator<GetPermissionsListQuery>;

public class GetPermissionsListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPermissionsListQuery, PagedListDto<PermissionDto>>
{
    public async Task<PagedListDto<PermissionDto>> Handle(GetPermissionsListQuery request, CancellationToken cancellationToken)
    {
        var dateRange = request.ToAuditDateRange();
        var paging = request.ToPagingOptions();

        var countSpec = new PermissionSpecification(request.Id, request.Name, request.Description,
            dateRange, PagingOptions.None, OrderingOptions.None, request.Visibility);
        long? totalCount = paging.IncludeTotalCount
            ? await unitOfWork.PermissionRepository.CountAsync(countSpec, cancellationToken)
            : null;

        var spec = new PermissionSpecification(request.Id, request.Name, request.Description,
            dateRange, paging, request.ToOrderingOptions(), request.Visibility);
        var entities = await unitOfWork.PermissionRepository.ListAsync(spec, cancellationToken);

        return request.ToPagedListDto(entities, entities.Adapt<List<PermissionDto>>(), totalCount);
    }
}

public class PermissionDto : AuditableDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
