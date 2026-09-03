using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Application.Permissions.Queries.GetPermissionsList;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Roles.Queries.GetRolePermissionsList;

public class GetRolePermissionsListQuery : GetBaseListQuery, IRequest<PagedListDto<PermissionDto>>
{
    public Guid RoleId { get; set; }
}

public class GetRolePermissionsListQueryValidator : AbstractValidator<GetRolePermissionsListQuery>;

public class GetRolePermissionsListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRolePermissionsListQuery, PagedListDto<PermissionDto>>
{
    public async Task<PagedListDto<PermissionDto>> Handle(GetRolePermissionsListQuery request,
        CancellationToken cancellationToken)
    {
        var countSpec = new RolePermissionSpecification(
            request.RoleId);
        countSpec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        
        var totalCount = await unitOfWork
            .RolePermissionRepository
            .CountAsync(
                countSpec,
                cancellationToken);
        
        var spec = new RolePermissionSpecification(request.RoleId, request.CurrentPage, request.PageSize,
            request.OrderBy);
        spec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        var entities = await unitOfWork.RolePermissionRepository.ListAsync(spec, cancellationToken);

        return new PagedListDto<PermissionDto>
        {
            Items = entities.Select(x=>x.Permission).Adapt<List<PermissionDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}