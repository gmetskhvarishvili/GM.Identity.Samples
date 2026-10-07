using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Permissions.Commands.HidePermission;

public class HidePermissionCommand : IRequest
{
    public Guid Id { get; set; }
}

public class HidePermissionCommandValidator : AbstractValidator<HidePermissionCommand>
{
    public HidePermissionCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class HidePermissionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<HidePermissionCommand>
{
    public async Task Handle(HidePermissionCommand request, CancellationToken cancellationToken)
    {
        // Fetch by id via the predicate path (no visibility filter), so the entity is found whatever its state.
        var entity = await unitOfWork.PermissionRepository
            .FirstOrDefaultAsync(x => x.Id == request.Id, true, null, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.Permission,
                StringResource.Id,
                request.Id);
        }

        entity.Hide();

        unitOfWork.PermissionRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
