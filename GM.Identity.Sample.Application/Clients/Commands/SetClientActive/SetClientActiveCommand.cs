using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Clients.Commands.SetClientActive;

/// <summary>
/// Activates or deactivates a client. Deactivating revokes the client's active sessions immediately, and a
/// deactivated client can no longer authenticate at /connect/token (the grant checks <c>IsActive</c>).
/// </summary>
public class SetClientActiveCommand : IRequest
{
    public Guid ClientId { get; set; }
    public bool Active { get; set; }
}

public class SetClientActiveCommandValidator : AbstractValidator<SetClientActiveCommand>
{
    public SetClientActiveCommandValidator() => RuleFor(x => x.ClientId).NotNull().NotEmpty();
}

public class SetClientActiveCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IRequestHandler<SetClientActiveCommand>
{
    public async Task Handle(SetClientActiveCommand request, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientRepository
            .FirstOrDefaultAsync(x => x.Id == request.ClientId && !x.IsDeleted, true, null, cancellationToken);

        if (client == null)
            throw new NotFoundException(StringResource.Client, StringResource.Id, request.ClientId);

        if (request.Active)
            client.Activate();
        else
            client.Deactivate();

        unitOfWork.ClientRepository.Update(client);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!request.Active)
            await unitOfWork.RevokeAllClientSessionsAsync(sessionCache, client.Id, cancellationToken);
    }
}
