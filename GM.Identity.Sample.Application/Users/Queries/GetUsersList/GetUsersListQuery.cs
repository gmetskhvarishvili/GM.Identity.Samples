using FluentValidation;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        var dateRange = request.ToAuditDateRange();
        var paging = request.ToPagingOptions();

        var countSpec = new UserSpecification(request.Id, request.Email, request.Username,
            dateRange, PagingOptions.None, OrderingOptions.None, request.Visibility);
        long? totalCount = paging.IncludeTotalCount
            ? await unitOfWork.UserRepository.CountAsync(countSpec, cancellationToken)
            : null;

        var spec = new UserSpecification(request.Id, request.Email, request.Username,
            dateRange, paging, request.ToOrderingOptions(), request.Visibility);
        var entities = await unitOfWork.UserRepository.ListAsync(spec, cancellationToken);

        return request.ToPagedListDto(entities, entities.Adapt<List<UserDto>>(), totalCount);
    }
}

public class UserDto : AuditableDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    /// <summary>Storage key of the user's profile image, if any.</summary>
    public string? ProfileImageKey { get; set; }
    public UserPersonalInfoDto? PersonalInfo { get; set; }
}
