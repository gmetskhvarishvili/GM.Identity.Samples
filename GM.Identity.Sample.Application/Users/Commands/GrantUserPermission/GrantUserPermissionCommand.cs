using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserPermissionAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.GrantUserPermission;

/// <summary>
/// Grants a permission directly to a user (in addition to role-derived permissions). Projected into the Redis
/// cache under the user's own id as a synthetic self-role, so the grant takes effect immediately at the gate.
/// Idempotent.
/// </summary>
public class GrantUserPermissionCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }
}

public class GrantUserPermissionCommandValidator : AbstractValidator<GrantUserPermissionCommand>
{
    public GrantUserPermissionCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.PermissionId).NotNull().NotEmpty();
    }
}

public class GrantUserPermissionCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<GrantUserPermissionCommand>
{
    public async Task Handle(GrantUserPermissionCommand request, CancellationToken cancellationToken)
    {
        var userExists = await unitOfWork.UserRepository.ExistsAsync(
            x => x.Id == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (!userExists)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        var permissionExists = await unitOfWork.PermissionRepository.ExistsAsync(
            x => x.Id == request.PermissionId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (!permissionExists)
            throw new NotFoundException(StringResource.Permission, StringResource.Id, request.PermissionId);

        if (await unitOfWork.UserPermissionRepository.ExistsAsync(
                x => x.UserId == request.UserId && x.PermissionId == request.PermissionId
                     && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
        {
            return;
        }

        await unitOfWork.UserPermissionRepository.AddAsync(
            UserPermission.Create(request.UserId, request.PermissionId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Write-through: the user's synthetic self-role (keyed by their own id) grants the permission.
        await permissionCache.AddUserRoleAsync(request.UserId, request.UserId, cancellationToken);
        await permissionCache.AddRolePermissionAsync(request.UserId, request.PermissionId, cancellationToken);
    }
}
