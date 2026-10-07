using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Operations.Commands.UnhideOperation;

public class UnhideOperationCommand : IRequest
{
    public Guid Id { get; set; }
}

public class UnhideOperationCommandValidator : AbstractValidator<UnhideOperationCommand>
{
    public UnhideOperationCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class UnhideOperationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UnhideOperationCommand>
{
    public async Task Handle(UnhideOperationCommand request, CancellationToken cancellationToken)
    {
        // Fetch by id via the predicate path (no visibility filter), so the entity is found whatever its state.
        var entity = await unitOfWork.OperationRepository
            .FirstOrDefaultAsync(x => x.Id == request.Id, true, null, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.Operation,
                StringResource.Id,
                request.Id);
        }

        entity.Unhide();

        unitOfWork.OperationRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
