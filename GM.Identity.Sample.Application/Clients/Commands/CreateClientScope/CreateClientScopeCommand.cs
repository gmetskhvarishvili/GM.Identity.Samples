using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Clients.Commands.CreateClientScope;

public class CreateClientScopeCommand : IRequest
{
    public Guid ClientId { get; set; }
    public Guid ScopeId { get; set; }
}

public class CreateClientScopeCommandValidator : AbstractValidator<CreateClientScopeCommand>
{
    public CreateClientScopeCommandValidator()
    {
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ScopeId).NotNull().NotEmpty();
    }
}

public class CreateClientScopeCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<CreateClientScopeCommand>
{
    public async Task Handle(CreateClientScopeCommand request, CancellationToken cancellationToken)
    {
        // Create the root aggregate
        var entity = ClientScope
            .Create(request.ClientId, request.ScopeId);

        if (await unitOfWork.ClientScopeRepository.ExistsAsync(
                x => x.ClientId == request.ClientId
                     && x.ScopeId == request.ScopeId
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.ClientScope,
                StringResource.ScopeId,
                request.ScopeId);
        }

        // Persist the aggregate
        await unitOfWork.ClientScopeRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}