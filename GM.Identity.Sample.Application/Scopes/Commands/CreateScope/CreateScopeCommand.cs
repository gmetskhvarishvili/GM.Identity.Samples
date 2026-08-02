using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Scopes.Commands.CreateScopeOperation;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ScopeOperationAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Scopes.Commands.CreateScope;

public class CreateScopeCommand : IRequest<string>
{
    public string? Name { get; set; }
    
    public IEnumerable<CreateScopeOperationCommand>? ScopeOperations { get; set; }
}

public class CreateScopeCommandValidator : AbstractValidator<CreateScopeCommand>
{
    public CreateScopeCommandValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}

public class CreateScopeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateScopeCommand, string>
{

    public async Task<string> Handle(CreateScopeCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.ScopeRepository.ExistsAsync(
                x => x.Name == request.Name
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.Scope,
                StringResource.Name,
                request.Name!);
        }
        
        // Create the root aggregate
        var entity = Scope
            .Create(request.Name!);
        
        // Add child items if any
        if (request.ScopeOperations?.Any() == true)
        {
            var items = request.ScopeOperations
                .Select(item => ScopeOperation.Create(entity.Id, item.OperationId!))
                .ToArray();
            
            entity.AddOperations(items);
        }

        // Persist the aggregate
        await unitOfWork.ScopeRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id.ToString();
    }
}