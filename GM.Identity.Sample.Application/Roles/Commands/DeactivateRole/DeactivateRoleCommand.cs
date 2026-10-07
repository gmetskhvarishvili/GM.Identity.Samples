using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Roles.Commands.DeactivateRole;

public class DeactivateRoleCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeactivateRoleCommandValidator : AbstractValidator<DeactivateRoleCommand>
{
    public DeactivateRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class DeactivateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeactivateRoleCommand>
{
    public async Task Handle(DeactivateRoleCommand request, CancellationToken cancellationToken)
    {
        // Fetch by id via the predicate path (no visibility filter), so the role is found whatever its state.
        var entity = await unitOfWork.RoleRepository
            .FirstOrDefaultAsync(x => x.Id == request.Id, true, null, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.Role,
                StringResource.Id,
                request.Id);
        }

        entity.Deactivate();

        unitOfWork.RoleRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
