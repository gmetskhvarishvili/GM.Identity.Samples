using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.MergeUsers;

/// <summary>
/// Merges a source user into a target: the source's roles, group memberships, and direct permissions are moved
/// to the target (deduplicated), the source's sessions are revoked, and the source account is soft-deleted.
/// The target's authorization projection is rebuilt so the moved access takes effect immediately.
/// </summary>
public class MergeUsersCommand : IRequest
{
    public Guid TargetUserId { get; set; }
    public Guid SourceUserId { get; set; }
}

public class MergeUsersCommandValidator : AbstractValidator<MergeUsersCommand>
{
    public MergeUsersCommandValidator()
    {
        RuleFor(x => x.TargetUserId).NotNull().NotEmpty();
        RuleFor(x => x.SourceUserId).NotNull().NotEmpty();
        RuleFor(x => x).Must(x => x.TargetUserId != x.SourceUserId).WithMessage("Cannot merge a user into itself.");
    }
}

public class MergeUsersCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache,
    IPermissionCache permissionCache) : IRequestHandler<MergeUsersCommand>
{
    public async Task Handle(MergeUsersCommand request, CancellationToken cancellationToken)
    {
        var target = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.TargetUserId && !x.IsDeleted, true, null, cancellationToken);
        if (target == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.TargetUserId);

        var source = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.SourceUserId && !x.IsDeleted, true, null, cancellationToken);
        if (source == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.SourceUserId);

        await MoveRolesAsync(request, cancellationToken);

        source.SoftRemove();
        unitOfWork.UserRepository.Update(source);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Source loses access immediately; target's projection is rebuilt to include the moved access.
        await unitOfWork.RevokeAllUserSessionsAsync(sessionCache, source.Id, cancellationToken);
        await permissionCache.RemoveUserAsync(source.Id, cancellationToken);

        await UserRoleProjection.RebuildUserAsync(unitOfWork, permissionCache, target.Id, cancellationToken);
    }

    private async Task MoveRolesAsync(MergeUsersCommand request, CancellationToken cancellationToken)
    {
        var targetRoleIds = (await unitOfWork.UserRoleRepository.Query(false, null)
            .Where(x => x.UserId == request.TargetUserId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.RoleId).ToListAsync(cancellationToken)).ToHashSet();

        var sourceRoles = await unitOfWork.UserRoleRepository.Query(true, null)
            .Where(x => x.UserId == request.SourceUserId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .ToListAsync(cancellationToken);

        foreach (var sourceRole in sourceRoles)
        {
            if (targetRoleIds.Add(sourceRole.RoleId))
                await unitOfWork.UserRoleRepository.AddAsync(
                    UserRole.Create(request.TargetUserId, sourceRole.RoleId), cancellationToken);
            unitOfWork.UserRoleRepository.Remove(sourceRole);
        }
    }
}
