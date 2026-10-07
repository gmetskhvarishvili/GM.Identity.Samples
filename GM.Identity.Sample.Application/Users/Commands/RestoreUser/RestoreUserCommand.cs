using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.RestoreUser;

public class RestoreUserCommand : IRequest
{
    public Guid Id { get; set; }
}

public class RestoreUserCommandValidator : AbstractValidator<RestoreUserCommand>
{
    public RestoreUserCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class RestoreUserCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<RestoreUserCommand>
{
    public async Task Handle(RestoreUserCommand request, CancellationToken cancellationToken)
    {
        // Fetch by id via the predicate path (no visibility filter), so the entity is found whatever its state.
        var entity = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.Id, true, null, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.User,
                StringResource.Id,
                request.Id);
        }

        entity.RestoreAll();

        unitOfWork.UserRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
