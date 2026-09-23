using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.SetUserActive;

/// <summary>Activates or deactivates a user. Deactivating revokes the user's active sessions immediately.</summary>
public class SetUserActiveCommand : IRequest
{
    public Guid UserId { get; set; }
    public bool Active { get; set; }
}

public class SetUserActiveCommandValidator : AbstractValidator<SetUserActiveCommand>
{
    public SetUserActiveCommandValidator() => RuleFor(x => x.UserId).NotNull().NotEmpty();
}

public class SetUserActiveCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SetUserActiveCommand>
{
    public async Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted, true, null, cancellationToken);

        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        if (request.Active)
            user.Activate();
        else
            user.Deactivate();

        unitOfWork.UserRepository.Update(user);

        // Deactivating revokes the user's sessions (which queue a SessionsRevoked outbox message for cache
        // eviction); stage them so they commit with the change in one save.
        if (!request.Active)
            await unitOfWork.UserSessionRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
