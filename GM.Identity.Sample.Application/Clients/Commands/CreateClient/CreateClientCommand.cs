using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Clients.Commands.CreateClientScope;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.ClientScopeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace GM.Identity.Sample.Application.Clients.Commands.CreateClient;

public class CreateClientCommand : IRequest<Guid>
{
    public string? Secret { get; set; }
    public string? Name { get; set; }

    public IEnumerable<CreateClientScopeCommand>? ClientScopes { get; set; }

    /// <summary>The exact URIs the authorization endpoint may redirect to (required for the authorization-code flow).</summary>
    public IEnumerable<string>? RedirectUris { get; set; }

    /// <summary>The client's OIDC back-channel logout endpoint, if any.</summary>
    public string? BackchannelLogoutUri { get; set; }

    /// <summary>The client's OIDC front-channel logout endpoint, if any.</summary>
    public string? FrontchannelLogoutUri { get; set; }

    /// <summary>Whether the authorization endpoint must obtain the user's consent for this client.</summary>
    public bool RequireConsent { get; set; }
}

public class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(x => x.Secret).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleForEach(x => x.RedirectUris)
            .Must(u => string.IsNullOrWhiteSpace(u) || ClientRedirectUris.IsAbsoluteUri(u.Trim()))
            .WithMessage("Each redirect URI must be an absolute URI.");
        RuleFor(x => x.BackchannelLogoutUri)
            .Must(ClientRedirectUris.IsAbsoluteUri).When(x => !string.IsNullOrWhiteSpace(x.BackchannelLogoutUri))
            .WithMessage("The back-channel logout URI must be an absolute URI.");
        RuleFor(x => x.FrontchannelLogoutUri)
            .Must(ClientRedirectUris.IsAbsoluteUri).When(x => !string.IsNullOrWhiteSpace(x.FrontchannelLogoutUri))
            .WithMessage("The front-channel logout URI must be an absolute URI.");
    }
}

public class CreateClientCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateClientCommand, Guid>
{
    public async Task<Guid> Handle(CreateClientCommand request, CancellationToken cancellationToken)
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

        if (!string.IsNullOrWhiteSpace(request.BackchannelLogoutUri))
            entity.SetBackchannelLogoutUri(request.BackchannelLogoutUri);
        if (!string.IsNullOrWhiteSpace(request.FrontchannelLogoutUri))
            entity.SetFrontchannelLogoutUri(request.FrontchannelLogoutUri);
        entity.SetRequireConsent(request.RequireConsent);

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
        await unitOfWork.AddClientRedirectUrisAsync(entity.Id, request.RedirectUris, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
