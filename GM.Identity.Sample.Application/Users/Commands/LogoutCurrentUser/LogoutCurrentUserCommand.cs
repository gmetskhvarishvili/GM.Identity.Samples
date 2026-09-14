using FluentValidation;
using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.LogoutCurrentUser;

/// <summary>Revokes the caller's current session (the one the presented token belongs to). Idempotent.</summary>
public class LogoutCurrentUserCommand : IRequest
{
    public Guid SessionId { get; set; }
}

public class LogoutCurrentUserCommandValidator : AbstractValidator<LogoutCurrentUserCommand>
{
    public LogoutCurrentUserCommandValidator() => RuleFor(x => x.SessionId).NotNull().NotEmpty();
}

public class LogoutCurrentUserCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IRequestHandler<LogoutCurrentUserCommand>
{
    public async Task Handle(LogoutCurrentUserCommand request, CancellationToken cancellationToken)
    {
        var session = await unitOfWork.UserSessionRepository
            .Query(true, null)
            .FirstOrDefaultAsync(x => x.Id == request.SessionId && !x.IsRevoked, cancellationToken);

        if (session == null) return; // already gone / unknown — nothing to do

        session.Revoke();
        unitOfWork.UserSessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await sessionCache.RemoveAsync(session.TokenHash, cancellationToken);
    }
}
