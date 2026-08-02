using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Operations.Commands.UpdateOperation;

public class UpdateOperationCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class UpdateOperationCommandValidator : AbstractValidator<UpdateOperationCommand>
{
    public UpdateOperationCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleFor(x => x.Description).NotNull().NotEmpty();
    }
}

public class UpdateOperationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateOperationCommand>
{
    public async Task Handle(UpdateOperationCommand request, CancellationToken cancellationToken)
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

        if (await unitOfWork.OperationRepository.ExistsAsync(
                x => x.Id != entity.Id
                     && x.Name == request.Name
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.Operation,
                StringResource.Name,
                request.Name!);
        }

        entity.Update(
            request.Name!,
            request.Description!);

        // Persist the aggregate
        unitOfWork.OperationRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}