using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Roles.Commands.RestoreRole;

public class RestoreRoleCommand : IRequest
{
    public Guid Id { get; set; }
}

public class RestoreRoleCommandValidator : AbstractValidator<RestoreRoleCommand>
{
    public RestoreRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class RestoreRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<RestoreRoleCommand>
{
    public async Task Handle(RestoreRoleCommand request, CancellationToken cancellationToken)
    {
        // Fetch by id via the predicate path, which applies no visibility filter — so a soft-deleted,
        // deactivated, or hidden role is returned (still tenant-scoped). RestoreAll() reactivates, undeletes,
        // and unhides it in one step.
        var entity = await unitOfWork.RoleRepository
            .FirstOrDefaultAsync(x => x.Id == request.Id, true, null, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.Role,
                StringResource.Id,
                request.Id);
        }

        entity.RestoreAll();

        unitOfWork.RoleRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
