using FluentValidation;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Groups.Commands.RemoveUserFromGroup;

/// <summary>Removes a user from a group; the user loses the group's roles (unless held another way). Idempotent.</summary>
public class RemoveUserFromGroupCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
}

public class RemoveUserFromGroupCommandValidator : AbstractValidator<RemoveUserFromGroupCommand>
{
    public RemoveUserFromGroupCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.GroupId).NotNull().NotEmpty();
    }
}

public class RemoveUserFromGroupCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<RemoveUserFromGroupCommand>
{
    public async Task Handle(RemoveUserFromGroupCommand request, CancellationToken cancellationToken)
    {
        var entity = await unitOfWork.UserGroupRepository
            .FirstOrDefaultAsync(
                x => x.UserId == request.UserId && x.GroupId == request.GroupId
                     && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);

        if (entity != null)
        {
            unitOfWork.UserGroupRepository.Remove(entity);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await UserRoleProjection.RebuildUserAsync(unitOfWork, permissionCache, request.UserId, cancellationToken);
    }
}
