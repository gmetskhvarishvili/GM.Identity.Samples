using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Operations.Commands.CreateOperation;

public class CreateOperationCommand : IRequest<string>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class CreateOperationCommandValidator : AbstractValidator<CreateOperationCommand>
{
    public CreateOperationCommandValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleFor(x => x.Description).NotNull().NotEmpty();
    }
}

public class CreateOperationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateOperationCommand, string>
{

    public async Task<string> Handle(CreateOperationCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.OperationRepository.ExistsAsync(
                x => x.Name == request.Name
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
        
        // Create the root aggregate
        var entity = Operation
            .Create(request.Name!, request.Description!);

        // Persist the aggregate
        await unitOfWork.OperationRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id.ToString();
    }
}