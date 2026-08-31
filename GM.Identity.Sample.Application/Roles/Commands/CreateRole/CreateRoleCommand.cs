using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Roles.Commands.CreateRolePermission;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RolePermissionAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Roles.Commands.CreateRole;

public class CreateRoleCommand : IRequest<string>
{
    public string? Name { get; set; }
    
    public IEnumerable<CreateRolePermissionCommand>? RolePermissions { get; set; }
}

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}

public class CreateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateRoleCommand, string>
{

    public async Task<string> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.RoleRepository.ExistsAsync(
                x => x.Name == request.Name
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.Role,
                StringResource.Name,
                request.Name!);
        }
        
        // Create the root aggregate
        var entity = Role
            .Create(request.Name!);
        
        // Add child items if any
        if (request.RolePermissions?.Any() == true)
        {
            var items = request.RolePermissions
                .Select(item => RolePermission.Create(entity.Id, item.PermissionId))
                .ToArray();
            
            entity.AddPermissions(items);
        }

        // Persist the aggregate
        await unitOfWork.RoleRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id.ToString();
    }
}