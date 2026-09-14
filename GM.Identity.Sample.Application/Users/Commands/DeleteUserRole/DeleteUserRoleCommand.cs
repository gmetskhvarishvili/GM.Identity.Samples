using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Users.Commands.DeleteUserRole;

public class DeleteUserRoleCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public class DeleteUserRoleCommandValidator : AbstractValidator<DeleteUserRoleCommand>
{
    public DeleteUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.RoleId).NotNull().NotEmpty();
    }
}

public class DeleteUserRoleCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<DeleteUserRoleCommand>
{
    public async Task Handle(DeleteUserRoleCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.UserRoleRepository
            .FirstOrDefaultAsync(x => x.UserId == request.UserId
                                      && x.RoleId == request.RoleId,
                true,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.UserRole,
                StringResource.RoleId,
                request.RoleId);
        }

        // Persist the aggregate
        unitOfWork.UserRoleRepository.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Write-through to the Redis RBAC projection.
        await permissionCache.RemoveUserRoleAsync(request.UserId, request.RoleId, cancellationToken);
    }
}
