using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Roles.Commands.DeleteRole;

public class DeleteRoleCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class DeleteRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteRoleCommand>
{
    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
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

        entity.SoftRemove();

        // Persist the aggregate
        unitOfWork.RoleRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}