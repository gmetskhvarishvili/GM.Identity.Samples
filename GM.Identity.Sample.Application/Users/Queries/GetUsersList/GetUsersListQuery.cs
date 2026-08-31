using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Users.Queries.GetUsersList;

public class GetUsersListQuery : GetBaseListQuery, IRequest<PagedListDto<UserDto>>
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
}

public class GetUsersListQueryValidator : AbstractValidator<GetUsersListQuery>;

public class GetUsersListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUsersListQuery, PagedListDto<UserDto>>
{
    public async Task<PagedListDto<UserDto>> Handle(GetUsersListQuery request, CancellationToken cancellationToken)
    {
        var countSpec = new UserSpecification(
            request.Id, request.Email, request.Username);
        
        var totalCount = await unitOfWork
            .UserRepository
            .CountAsync(
                countSpec,
                cancellationToken);
        
        var spec = new UserSpecification(request.Id, request.Email, request.Username, request.CurrentPage, request.PageSize,
            request.OrderBy ?? string.Empty);
        var entities = await unitOfWork.UserRepository.ListAsync(spec, cancellationToken);

        return new PagedListDto<UserDto>
        {
            Items = entities.Adapt<List<UserDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
}