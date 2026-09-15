using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.TimeBoundRoleGrantAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.GrantTimeBoundRole;

/// <summary>
/// Assigns a role to a user until an expiry time (temporary/elevated access). The role is effective immediately
/// and stops granting access once expired; the reconcile job and purge job clean up expired grants.
/// </summary>
public class GrantTimeBoundRoleCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class GrantTimeBoundRoleCommandValidator : AbstractValidator<GrantTimeBoundRoleCommand>
{
    public GrantTimeBoundRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.RoleId).NotNull().NotEmpty();
        RuleFor(x => x.ExpiresAt).GreaterThan(_ => DateTime.UtcNow)
            .WithMessage("The expiry must be in the future.");
    }
}

public class GrantTimeBoundRoleCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<GrantTimeBoundRoleCommand>
{
    public async Task Handle(GrantTimeBoundRoleCommand request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.UserRepository.ExistsAsync(
                x => x.Id == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);
        if (!await unitOfWork.RoleRepository.ExistsAsync(
                x => x.Id == request.RoleId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
            throw new NotFoundException(StringResource.Role, StringResource.Id, request.RoleId);

        await unitOfWork.TimeBoundRoleGrantRepository.AddAsync(
            TimeBoundRoleGrant.Create(request.UserId, request.RoleId, request.ExpiresAt), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await UserRoleProjection.RebuildUserAsync(unitOfWork, permissionCache, request.UserId, cancellationToken);
    }
}
