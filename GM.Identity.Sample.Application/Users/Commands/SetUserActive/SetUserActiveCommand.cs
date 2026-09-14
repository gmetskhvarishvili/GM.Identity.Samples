using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
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

public class SetUserActiveCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IRequestHandler<SetUserActiveCommand>
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
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!request.Active)
            await unitOfWork.RevokeAllUserSessionsAsync(sessionCache, user.Id, cancellationToken);
    }
}
