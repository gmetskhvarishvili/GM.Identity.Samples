using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Roles.Queries.GetRolesList;

public class GetRolesListQuery : GetBaseListQuery, IRequest<PagedListDto<RoleDto>>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class GetRolesListQueryValidator : AbstractValidator<GetRolesListQuery>;

public class GetRolesListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRolesListQuery, PagedListDto<RoleDto>>
{
    public async Task<PagedListDto<RoleDto>> Handle(GetRolesListQuery request, CancellationToken cancellationToken)
    {
        var countSpec = new RoleSpecification(
            request.Id, 
            request.Name);
        countSpec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        
        var totalCount = await unitOfWork
            .RoleRepository
            .CountAsync(
                countSpec,
                cancellationToken);
        
        var spec = new RoleSpecification(request.Id, request.Name, request.CurrentPage, request.PageSize,
            request.OrderBy);
        spec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        var entities = await unitOfWork.RoleRepository.ListAsync(spec, cancellationToken);

        return new PagedListDto<RoleDto>
        {
            Items = entities.Adapt<List<RoleDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}

public class RoleDto : AuditableDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}