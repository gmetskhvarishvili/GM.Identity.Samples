using FluentValidation;
using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.RevokeUserPermission;

/// <summary>Revokes a directly-granted user permission. Idempotent — a no-op if the grant is absent.</summary>
public class RevokeUserPermissionCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }
}

public class RevokeUserPermissionCommandValidator : AbstractValidator<RevokeUserPermissionCommand>
{
    public RevokeUserPermissionCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.PermissionId).NotNull().NotEmpty();
    }
}

public class RevokeUserPermissionCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<RevokeUserPermissionCommand>
{
    public async Task Handle(RevokeUserPermissionCommand request, CancellationToken cancellationToken)
    {
        var entity = await unitOfWork.UserPermissionRepository
            .FirstOrDefaultAsync(
                x => x.UserId == request.UserId && x.PermissionId == request.PermissionId
                     && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);
        if (entity == null)
            return;

        unitOfWork.UserPermissionRepository.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await permissionCache.RemoveRolePermissionAsync(request.UserId, request.PermissionId, cancellationToken);

        // If no direct permissions remain, drop the synthetic self-role from the user's role set.
        var hasMore = await unitOfWork.UserPermissionRepository.ExistsAsync(
            x => x.UserId == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (!hasMore)
            await permissionCache.RemoveUserRoleAsync(request.UserId, request.UserId, cancellationToken);
    }
}
