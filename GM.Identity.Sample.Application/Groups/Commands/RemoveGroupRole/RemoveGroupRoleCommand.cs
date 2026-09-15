using FluentValidation;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Groups.Commands.RemoveGroupRole;

/// <summary>Removes a role from a group; members lose it (unless held another way). Idempotent.</summary>
public class RemoveGroupRoleCommand : IRequest
{
    public Guid GroupId { get; set; }
    public Guid RoleId { get; set; }
}

public class RemoveGroupRoleCommandValidator : AbstractValidator<RemoveGroupRoleCommand>
{
    public RemoveGroupRoleCommandValidator()
    {
        RuleFor(x => x.GroupId).NotNull().NotEmpty();
        RuleFor(x => x.RoleId).NotNull().NotEmpty();
    }
}

public class RemoveGroupRoleCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<RemoveGroupRoleCommand>
{
    public async Task Handle(RemoveGroupRoleCommand request, CancellationToken cancellationToken)
    {
        var entity = await unitOfWork.GroupRoleRepository
            .FirstOrDefaultAsync(
                x => x.GroupId == request.GroupId && x.RoleId == request.RoleId
                     && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);

        // Rebuild members whether or not the row existed (keeps the cache consistent).
        if (entity != null)
        {
            unitOfWork.GroupRoleRepository.Remove(entity);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await UserRoleProjection.RebuildGroupMembersAsync(unitOfWork, permissionCache, request.GroupId, cancellationToken);
    }
}
