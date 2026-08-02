using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Roles.Commands.DeleteRolePermission;

public class DeleteRolePermissionCommand : IRequest
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

public class DeleteRolePermissionCommandValidator : AbstractValidator<DeleteRolePermissionCommand>
{
    public DeleteRolePermissionCommandValidator()
    {
        RuleFor(x => x.RoleId).NotNull().NotEmpty();
        RuleFor(x => x.PermissionId).NotNull().NotEmpty();
    }
}

public class DeleteRolePermissionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteRolePermissionCommand>
{
    public async Task Handle(DeleteRolePermissionCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.RolePermissionRepository
            .FirstOrDefaultAsync(x => x.RoleId == request.RoleId
                                      && x.PermissionId == request.PermissionId
                                      && x.IsActive
                                      && !x.IsDeleted
                                      && !x.IsHidden,
                true,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.RolePermission,
                StringResource.PermissionId,
                request.PermissionId!);
        }

        entity.SoftRemove();

        // Persist the aggregate
        unitOfWork.RolePermissionRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}