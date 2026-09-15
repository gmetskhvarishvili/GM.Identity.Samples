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

namespace GM.Identity.Sample.Application.Users.Commands.RevokeTimeBoundRole;

/// <summary>Revokes any time-bound grants of a role to a user before they expire. Idempotent.</summary>
public class RevokeTimeBoundRoleCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public class RevokeTimeBoundRoleCommandValidator : AbstractValidator<RevokeTimeBoundRoleCommand>
{
    public RevokeTimeBoundRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.RoleId).NotNull().NotEmpty();
    }
}

public class RevokeTimeBoundRoleCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<RevokeTimeBoundRoleCommand>
{
    public async Task Handle(RevokeTimeBoundRoleCommand request, CancellationToken cancellationToken)
    {
        var grants = await unitOfWork.TimeBoundRoleGrantRepository
            .Query(true, null)
            .Where(x => x.UserId == request.UserId && x.RoleId == request.RoleId
                        && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .ToListAsync(cancellationToken);

        if (grants.Count > 0)
        {
            unitOfWork.TimeBoundRoleGrantRepository.RemoveRange(grants);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await UserRoleProjection.RebuildUserAsync(unitOfWork, permissionCache, request.UserId, cancellationToken);
    }
}
