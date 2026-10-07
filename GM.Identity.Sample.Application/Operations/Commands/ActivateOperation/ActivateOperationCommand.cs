using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Operations.Commands.ActivateOperation;

public class ActivateOperationCommand : IRequest
{
    public Guid Id { get; set; }
}

public class ActivateOperationCommandValidator : AbstractValidator<ActivateOperationCommand>
{
    public ActivateOperationCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class ActivateOperationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ActivateOperationCommand>
{
    public async Task Handle(ActivateOperationCommand request, CancellationToken cancellationToken)
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

        entity.Activate();

        unitOfWork.OperationRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
