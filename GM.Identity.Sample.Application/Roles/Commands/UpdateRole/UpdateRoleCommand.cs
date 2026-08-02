using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Roles.Commands.UpdateRole;

public class UpdateRoleCommand : IRequest
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
}

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}

public class UpdateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateRoleCommand>
{
    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.RoleRepository
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
                StringResource.Role,
                StringResource.Id,
                request.Id!);
        }

        if (await unitOfWork.RoleRepository.ExistsAsync(
                x => x.Id != entity.Id
                     && x.Name == request.Name
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

        entity.Update(request.Name!);

        // Persist the aggregate
        unitOfWork.RoleRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}