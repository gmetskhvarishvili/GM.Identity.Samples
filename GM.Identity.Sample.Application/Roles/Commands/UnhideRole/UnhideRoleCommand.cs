using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Roles.Commands.UnhideRole;

public class UnhideRoleCommand : IRequest
{
    public Guid Id { get; set; }
}

public class UnhideRoleCommandValidator : AbstractValidator<UnhideRoleCommand>
{
    public UnhideRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class UnhideRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UnhideRoleCommand>
{
    public async Task Handle(UnhideRoleCommand request, CancellationToken cancellationToken)
    {
        // Fetch by id via the predicate path (no visibility filter), so a hidden role is found.
        var entity = await unitOfWork.RoleRepository
            .FirstOrDefaultAsync(x => x.Id == request.Id, true, null, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.Role,
                StringResource.Id,
                request.Id);
        }

        entity.Unhide();

        unitOfWork.RoleRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
