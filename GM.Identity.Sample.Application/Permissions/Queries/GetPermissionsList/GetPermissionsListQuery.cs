using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

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
        var countSpec = new PermissionSpecification(
            request.Id, 
            request.Name,
            request.Description);
        countSpec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        
        var totalCount = await unitOfWork
            .PermissionRepository
            .CountAsync(
                countSpec,
                cancellationToken);

        
        var spec = new PermissionSpecification(request.Id, request.Name, request.Description, request.CurrentPage, request.PageSize, request.OrderBy);
        spec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
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

