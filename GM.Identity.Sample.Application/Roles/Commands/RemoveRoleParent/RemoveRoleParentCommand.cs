using FluentValidation;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Roles.Commands.RemoveRoleParent;

/// <summary>Removes a role's parent edge (it no longer inherits). Reprojects the effective cache. Idempotent.</summary>
public class RemoveRoleParentCommand : IRequest
{
    public Guid RoleId { get; set; }
}

public class RemoveRoleParentCommandValidator : AbstractValidator<RemoveRoleParentCommand>
{
    public RemoveRoleParentCommandValidator() => RuleFor(x => x.RoleId).NotNull().NotEmpty();
}

public class RemoveRoleParentCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<RemoveRoleParentCommand>
{
    public async Task Handle(RemoveRoleParentCommand request, CancellationToken cancellationToken)
    {
        var edges = await unitOfWork.RoleHierarchyRepository
            .Query(true, null)
            .Where(x => x.RoleId == request.RoleId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .ToListAsync(cancellationToken);

        if (edges.Count == 0)
            return;

        unitOfWork.RoleHierarchyRepository.RemoveRange(edges);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await RoleHierarchyProjection.ReprojectAsync(unitOfWork, permissionCache, cancellationToken);

        // The unparented role's effective set may now be empty (no parent, maybe no own perms). Reprojection
        // only emits roles it can compute, so explicitly rewrite this role's own permissions to overwrite any
        // stale inherited entry left in the cache.
        var ownPermissions = await unitOfWork.RolePermissionRepository
            .Query(false, null)
            .Where(x => x.RoleId == request.RoleId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);
        await permissionCache.ReplaceRolePermissionsAsync(request.RoleId, ownPermissions, cancellationToken);
    }
}
