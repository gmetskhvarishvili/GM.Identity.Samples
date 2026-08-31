using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using ValidationException = GM.Exceptions.ValidationException;

namespace GM.Identity.Sample.Application.Scopes.Commands.CreateScopeOperation;

public class CreateScopeOperationCommand : IRequest<string>
{
    public Guid ScopeId { get; set; }
    public Guid OperationId { get; set; }
}

public class CreateScopeOperationCommandValidator : AbstractValidator<CreateScopeOperationCommand>
{
    public CreateScopeOperationCommandValidator()
    {
        RuleFor(x => x.ScopeId).NotNull().NotEmpty();
        RuleFor(x => x.OperationId).NotNull().NotEmpty();
    }
}

public class CreateScopeOperationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateScopeOperationCommand, string>
{

    public async Task<string> Handle(CreateScopeOperationCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.ScopeOperationRepository.ExistsAsync(
                x => x.ScopeId == request.ScopeId
                     && x.OperationId == request.OperationId
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.ScopeOperation,
                StringResource.OperationId,
                request.OperationId);
        }
        
        // Create the root aggregate
        var entity = ScopeOperation
            .Create(request.ScopeId,
                request.OperationId);

        // Persist the aggregate
        await unitOfWork.ScopeOperationRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id.ToString();
    }
}