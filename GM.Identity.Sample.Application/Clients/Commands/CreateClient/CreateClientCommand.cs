using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Clients.Commands.CreateClientScope;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Clients.Commands.CreateClient;

public class CreateClientCommand : IRequest<string>
{
    public string? Secret { get; set; }
    public string? Name { get; set; }
    
    public IEnumerable<CreateClientScopeCommand>? ClientScopes { get; set; }
}

public class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(x => x.Secret).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}

public class CreateClientCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateClientCommand, string>
{
    public async Task<string> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.ClientRepository.ExistsAsync(
                x => x.Name == request.Name
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.Client,
                StringResource.Name,
                request.Name!);
        }

        // Create the root aggregate
        var entity = Client
            .Create(request.Name!);
        
        var (hash, salt) = PasswordHasher
            .Hash(request.Secret!);
        
        entity.UpdateSecret(hash, salt);
        
        // Add child items if any
        if (request.ClientScopes?.Any() == true)
        {
            var items = request.ClientScopes
                .Select(item =>
                    ClientScope.Create(entity.Id, item.ScopeId))
                .ToArray();

            entity.AddScopes(items);
        }

        // Persist the aggregate
        await unitOfWork.ClientRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id.ToString();
    }
}