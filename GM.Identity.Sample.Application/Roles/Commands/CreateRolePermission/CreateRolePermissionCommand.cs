using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common.Authorization;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Roles.Commands.CreateRolePermission;

public class CreateRolePermissionCommand : IRequest<Guid>
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

public class CreateRolePermissionCommandValidator : AbstractValidator<CreateRolePermissionCommand>
{
    public CreateRolePermissionCommandValidator()
    {
        RuleFor(x => x.RoleId).NotNull().NotEmpty();
        RuleFor(x => x.PermissionId).NotNull();
    }
}

public class CreateRolePermissionCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<CreateRolePermissionCommand, Guid>
{

    public async Task<Guid> Handle(CreateRolePermissionCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.RolePermissionRepository.ExistsAsync(
                x => x.RoleId == request.RoleId
                     && x.PermissionId == request.PermissionId
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.RolePermission,
                StringResource.PermissionId,
                request.PermissionId);
        }
        
        // Create the root aggregate
        var entity = RolePermission
            .Create(request.RoleId,
                request.PermissionId);

        // Persist the aggregate
        await unitOfWork.RolePermissionRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Write-through to the Redis RBAC projection.
        await permissionCache.AddRolePermissionAsync(request.RoleId, request.PermissionId, cancellationToken);

        return entity.Id;
    }
}
