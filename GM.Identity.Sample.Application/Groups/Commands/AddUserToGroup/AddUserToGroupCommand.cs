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

namespace GM.Identity.Sample.Application.Groups.Commands.AddUserToGroup;

/// <summary>Adds a user to a group; the user inherits the group's roles immediately. Idempotent.</summary>
public class AddUserToGroupCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
}

public class AddUserToGroupCommandValidator : AbstractValidator<AddUserToGroupCommand>
{
    public AddUserToGroupCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.GroupId).NotNull().NotEmpty();
    }
}

public class AddUserToGroupCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<AddUserToGroupCommand>
{
    public async Task Handle(AddUserToGroupCommand request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.UserRepository.ExistsAsync(
                x => x.Id == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);
        if (!await unitOfWork.GroupRepository.ExistsAsync(
                x => x.Id == request.GroupId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
            throw new NotFoundException(StringResource.Name, StringResource.Id, request.GroupId);

        if (!await unitOfWork.UserGroupRepository.ExistsAsync(
                x => x.UserId == request.UserId && x.GroupId == request.GroupId
                     && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
        {
            await unitOfWork.UserGroupRepository.AddAsync(
                UserGroup.Create(request.UserId, request.GroupId), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await UserRoleProjection.RebuildUserAsync(unitOfWork, permissionCache, request.UserId, cancellationToken);
    }
}
