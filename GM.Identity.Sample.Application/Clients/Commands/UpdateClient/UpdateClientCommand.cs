using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Clients.Commands.UpdateClient;

public class UpdateClientCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Secret { get; set; }
    public string? Name { get; set; }

    /// <summary>The client's OIDC back-channel logout endpoint. Pass empty to clear it; null leaves it unchanged.</summary>
    public string? BackchannelLogoutUri { get; set; }

    /// <summary>The client's OIDC front-channel logout endpoint. Pass empty to clear it; null leaves it unchanged.</summary>
    public string? FrontchannelLogoutUri { get; set; }
}

public class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
        RuleFor(x => x.Secret).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}

public class UpdateClientCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateClientCommand>
{
    public async Task Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.ClientRepository
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
                StringResource.Client,
                StringResource.Id,
                request.Id);
        }

        entity.Update(
            request.Name!);

        if (request.BackchannelLogoutUri is not null)
            entity.SetBackchannelLogoutUri(
                request.BackchannelLogoutUri.Length == 0 ? null : request.BackchannelLogoutUri);

        if (request.FrontchannelLogoutUri is not null)
            entity.SetFrontchannelLogoutUri(
                request.FrontchannelLogoutUri.Length == 0 ? null : request.FrontchannelLogoutUri);

        // Persist the aggregate
        unitOfWork.ClientRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
