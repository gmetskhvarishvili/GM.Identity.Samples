using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Application.Roles.Queries.GetRolesList;
using GM.Identity.Sample.Application.Users.Queries.GetUsersList;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Users.Queries.GetUserRolesList;

public class GetUserRolesListQuery : GetBaseListQuery, IRequest<PagedListDto<RoleDto>>
{
    public Guid UserId { get; set; }
}

public class GetUserRolesListQueryValidator : AbstractValidator<GetUserRolesListQuery>
{
    public GetUserRolesListQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GetUserRolesListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUserRolesListQuery, PagedListDto<RoleDto>>
{
    public async Task<PagedListDto<RoleDto>> Handle(GetUserRolesListQuery request, CancellationToken cancellationToken)
    {
        var countSpec = new UserRoleSpecification(
            request.UserId);
        
        var totalCount = await unitOfWork
            .UserRoleRepository
            .CountAsync(
                countSpec,
                cancellationToken);
        
        var spec = new UserRoleSpecification(request.UserId, request.CurrentPage, request.PageSize,
            request.OrderBy);
        var entities = await unitOfWork.UserRoleRepository.ListAsync(spec, cancellationToken);

        return new PagedListDto<RoleDto>
        {
            Items = entities.Select(x=>x.Role).Adapt<List<RoleDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}