using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common.Authorization;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Users.Commands.CreateUserRole;

public class CreateUserRoleCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public class CreateUserRoleCommandValidator : AbstractValidator<CreateUserRoleCommand>
{
    public CreateUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.RoleId).NotNull().NotEmpty();
    }
}

public class CreateUserRoleCommandHandler(
    IUnitOfWork unitOfWork,
    IPermissionCache permissionCache) : IRequestHandler<CreateUserRoleCommand>
{
    public async Task Handle(CreateUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.UserRoleRepository.ExistsAsync(
                x => x.UserId == request.UserId
                     && x.RoleId == request.RoleId
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.UserRole,
                StringResource.RoleId,
                request.RoleId);
        }
        
        
        // Create the root aggregate
        var entity = UserRole
            .Create(request.UserId, request.RoleId);


        // Persist the aggregate
        await unitOfWork.UserRoleRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Write-through to the Redis RBAC projection.
        await permissionCache.AddUserRoleAsync(request.UserId, request.RoleId, cancellationToken);
    }
}
