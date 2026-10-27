using FluentValidation;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Queries.GetMyPermissions;

/// <summary>
/// The names of every permission the user holds through their active roles, for a front end to cache and check
/// locally (show/hide UI). The API still enforces each permission itself, so this list only drives the UI.
/// </summary>
public class GetMyPermissionsQuery : IRequest<IReadOnlyCollection<string>>
{
    public Guid UserId { get; set; }
}

public class GetMyPermissionsQueryValidator : AbstractValidator<GetMyPermissionsQuery>
{
    public GetMyPermissionsQueryValidator() => RuleFor(x => x.UserId).NotEmpty();
}

public class GetMyPermissionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyPermissionsQuery, IReadOnlyCollection<string>>
{
    public async Task<IReadOnlyCollection<string>> Handle(GetMyPermissionsQuery request, CancellationToken cancellationToken)
    {
        // Only links and records that grant access count: the user's active roles, the roles' active permission
        // links, and active permissions — the same set the RBAC projection answers HasPermission from.
        var roleIds = unitOfWork.UserRoleRepository.Query(false, null)
            .Where(ur => ur.UserId == request.UserId && ur.IsActive && !ur.IsDeleted)
            .Select(ur => ur.RoleId)
            .Intersect(unitOfWork.RoleRepository.Query(false, null)
                .Where(r => r.IsActive && !r.IsDeleted)
                .Select(r => r.Id));

        var permissionIds = unitOfWork.RolePermissionRepository.Query(false, null)
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsActive && !rp.IsDeleted)
            .Select(rp => rp.PermissionId);

        return await unitOfWork.PermissionRepository.Query(false, null)
            .Where(p => permissionIds.Contains(p.Id) && p.IsActive && !p.IsDeleted)
            .Select(p => p.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }
}
