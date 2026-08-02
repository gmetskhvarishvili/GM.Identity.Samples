using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.PermissionAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Permissions.Commands.CreatePermission;

public class CreatePermissionCommand : IRequest<string>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class CreatePermissionCommandValidator : AbstractValidator<CreatePermissionCommand>
{
    public CreatePermissionCommandValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleFor(x => x.Description).NotNull().NotEmpty();
    }
}

public class CreatePermissionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreatePermissionCommand, string>
{

    public async Task<string> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.PermissionRepository.ExistsAsync(
                x => x.Name == request.Name
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.Permission,
                StringResource.Name,
                request.Name!);
        }
        
        // Create the root aggregate
        var entity = Permission
            .Create(request.Name!, request.Description!);

        // Persist the aggregate
        await unitOfWork.PermissionRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id.ToString();
    }
}