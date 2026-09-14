using FluentValidation;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Application.Common;
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

        var countSpec = new PermissionSpecification(request.Id, request.Name, request.Description,
            dateRange, PagingOptions.None, OrderingOptions.None);
        var totalCount = await unitOfWork.PermissionRepository.CountAsync(countSpec, cancellationToken);

        var spec = new PermissionSpecification(request.Id, request.Name, request.Description,
            dateRange, request.ToPagingOptions(), request.ToOrderingOptions());
        var entities = await unitOfWork.PermissionRepository.ListAsync(spec, cancellationToken);

        return new PagedListDto<PermissionDto>
        {
            Items = entities.Adapt<List<PermissionDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}

public class PermissionDto : AuditableDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
