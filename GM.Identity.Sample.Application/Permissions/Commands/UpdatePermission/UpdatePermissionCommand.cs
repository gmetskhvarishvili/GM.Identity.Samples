using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Permissions.Commands.UpdatePermission;

public class UpdatePermissionCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class UpdatePermissionCommandValidator : AbstractValidator<UpdatePermissionCommand>
{
    public UpdatePermissionCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleFor(x => x.Description).NotNull().NotEmpty();
    }
}

public class UpdatePermissionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdatePermissionCommand>
{
    public async Task Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.PermissionRepository
            .FirstOrDefaultAsync(x => x.Id == request.Id
                                      && x.IsActive
                                      && !x.IsDeleted
                                      && !x.IsHidden,
                true,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.Permission,
                StringResource.Id,
                request.Id!);
        }

        if (await unitOfWork.PermissionRepository.ExistsAsync(
                x => x.Id != entity.Id
                     && x.Name == request.Name
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

        entity.Update(
            request.Name!,
            request.Description!);

        // Persist the aggregate
        unitOfWork.PermissionRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}