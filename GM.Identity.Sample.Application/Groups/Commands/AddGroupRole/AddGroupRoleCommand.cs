using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.GroupAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Groups.Commands.AddGroupRole;

/// <summary>Grants a role to a group; every current member inherits it immediately. Idempotent.</summary>
public class AddGroupRoleCommand : IRequest
{
    public Guid GroupId { get; set; }
    public Guid RoleId { get; set; }
}

public class AddGroupRoleCommandValidator : AbstractValidator<AddGroupRoleCommand>
{
    public AddGroupRoleCommandValidator()
    {
        RuleFor(x => x.GroupId).NotNull().NotEmpty();
        RuleFor(x => x.RoleId).NotNull().NotEmpty();
    }
}

public class AddGroupRoleCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<AddGroupRoleCommand>
{
    public async Task Handle(AddGroupRoleCommand request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.GroupRepository.ExistsAsync(
                x => x.Id == request.GroupId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
            throw new NotFoundException(StringResource.Name, StringResource.Id, request.GroupId);
        if (!await unitOfWork.RoleRepository.ExistsAsync(
                x => x.Id == request.RoleId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
            throw new NotFoundException(StringResource.Role, StringResource.Id, request.RoleId);

        if (!await unitOfWork.GroupRoleRepository.ExistsAsync(
                x => x.GroupId == request.GroupId && x.RoleId == request.RoleId
                     && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
        {
            await unitOfWork.GroupRoleRepository.AddAsync(
                GroupRole.Create(request.GroupId, request.RoleId), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await UserRoleProjection.RebuildGroupMembersAsync(unitOfWork, permissionCache, request.GroupId, cancellationToken);
    }
}
