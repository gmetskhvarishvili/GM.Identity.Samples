using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleHierarchyAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Roles.Commands.SetRoleParent;

/// <summary>
/// Sets (or replaces) a role's parent in the hierarchy, so the role inherits the parent's permissions
/// transitively. Rejects a parent that would create a cycle. Reprojects the effective role→permission cache.
/// </summary>
public class SetRoleParentCommand : IRequest
{
    public Guid RoleId { get; set; }
    public Guid ParentRoleId { get; set; }
}

public class SetRoleParentCommandValidator : AbstractValidator<SetRoleParentCommand>
{
    public SetRoleParentCommandValidator()
    {
        RuleFor(x => x.RoleId).NotNull().NotEmpty();
        RuleFor(x => x.ParentRoleId).NotNull().NotEmpty();
        RuleFor(x => x).Must(x => x.RoleId != x.ParentRoleId).WithMessage("A role cannot be its own parent.");
    }
}

public class SetRoleParentCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<SetRoleParentCommand>
{
    public async Task Handle(SetRoleParentCommand request, CancellationToken cancellationToken)
    {
        foreach (var id in new[] { request.RoleId, request.ParentRoleId })
        {
            if (!await unitOfWork.RoleRepository.ExistsAsync(
                    x => x.Id == id && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
                throw new NotFoundException(StringResource.Role, StringResource.Id, id);
        }

        var edges = (await unitOfWork.RoleHierarchyRepository.GetAllAsync(true, null, cancellationToken))
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden)
            .ToList();
        var parentByRole = edges.ToDictionary(x => x.RoleId, x => x.ParentRoleId);

        // Cycle check: walking ancestors of the proposed parent must not reach the role.
        var cursor = request.ParentRoleId;
        var seen = new HashSet<Guid>();
        while (seen.Add(cursor))
        {
            if (cursor == request.RoleId)
                throw new ValidationException("That parent would create a cycle in the role hierarchy.");
            if (!parentByRole.TryGetValue(cursor, out var next))
                break;
            cursor = next;
        }

        // Replace any existing parent edge for the role.
        var existing = edges.Where(x => x.RoleId == request.RoleId).ToList();
        if (existing.Count > 0)
            unitOfWork.RoleHierarchyRepository.RemoveRange(existing);
        await unitOfWork.RoleHierarchyRepository.AddAsync(
            RoleHierarchy.Create(request.RoleId, request.ParentRoleId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await RoleHierarchyProjection.ReprojectAsync(unitOfWork, permissionCache, cancellationToken);
    }
}
