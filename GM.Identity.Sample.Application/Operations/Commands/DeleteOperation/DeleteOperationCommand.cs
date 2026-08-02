using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Operations.Commands.DeleteOperation;

public class DeleteOperationCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteOperationCommandValidator : AbstractValidator<DeleteOperationCommand>
{
    public DeleteOperationCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class DeleteOperationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteOperationCommand>
{
    public async Task Handle(DeleteOperationCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.OperationRepository
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
                StringResource.Operation,
                StringResource.Id,
                request.Id!);
        }

        entity.SoftRemove();

        // Persist the aggregate
        unitOfWork.OperationRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}