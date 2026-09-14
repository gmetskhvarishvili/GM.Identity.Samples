using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common.Authorization;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.OperationAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Operations.Commands.CreateOperation;

public class CreateOperationCommand : IRequest<Guid>
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

public class CreateOperationCommandHandler(
    IUnitOfWork unitOfWork,
    IScopeCache scopeCache) : IRequestHandler<CreateOperationCommand, Guid>
{

    public async Task<Guid> Handle(CreateOperationCommand request, CancellationToken cancellationToken)
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

        // Write-through: expose the operation name → id so [RequiresScope] can resolve it immediately.
        await scopeCache.SetOperationIdAsync(entity.Name!, entity.Id, cancellationToken);

        return entity.Id;
    }
}
