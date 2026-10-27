using FluentValidation;
using GM.Exceptions;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Application.Users;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Queries.GetUserDetails;

public class GetUserDetailsQuery : GetBaseDetailsQuery, IRequest<UserDetailsDto>
{
}

public class GetUserDetailsQueryValidator : AbstractValidator<GetUserDetailsQuery>
{
    public GetUserDetailsQueryValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class GetUserDetailsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetUserDetailsQuery, UserDetailsDto>
{
    public async Task<UserDetailsDto> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.UserRepository
            .Query(true, null)
            .Include(x => x.PersonalInfo)
            .Include(x => x.UserRoles)
            .Include(x => x.UserTwoFactorAuthTypes)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == request.Id
                                      && (request.Visibility.HasFlag(VisibilityScope.IncludeInactive) || x.IsActive)
                                      && (request.Visibility.HasFlag(VisibilityScope.IncludeDeleted) || !x.IsDeleted)
                                      && (request.Visibility.HasFlag(VisibilityScope.IncludeHidden) || !x.IsHidden),
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.User,
                StringResource.Id,
                request.Id!);
        }

        var result = entity.Adapt<UserDetailsDto>();

        return result;
    }
}

/// <summary>
/// Everything about the user except the password hash and salt (never returned) and sessions (their own endpoint).
/// </summary>
public class UserDetailsDto : AuditableDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? EmailConfirmedAt { get; set; }
    public string? Username { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public string? PhoneNumberConfirmedAt { get; set; }
    public bool LockoutEnabled { get; set; }
    public string? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockedAt { get; set; }

    /// <summary>The user's active roles.</summary>
    public IReadOnlyCollection<Guid> RoleIds { get; set; } = [];

    /// <summary>The user's active second-factor methods (1 Email, 2 SMS, 3 WhatsApp, or a custom type).</summary>
    public IReadOnlyCollection<int> TwoFactorAuthTypeIds { get; set; } = [];

    /// <summary>Storage key of the user's profile image, if any.</summary>
    public string? ProfileImageKey { get; set; }
    public UserPersonalInfoDto? PersonalInfo { get; set; }
}
