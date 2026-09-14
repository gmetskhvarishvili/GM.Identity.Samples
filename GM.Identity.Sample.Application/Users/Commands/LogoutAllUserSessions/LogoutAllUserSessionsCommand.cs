using FluentValidation;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.LogoutAllUserSessions;

/// <summary>
/// Logs a user out everywhere — revokes every one of their active sessions (DB + cache). Backs the "sign out
/// of all devices" self-service action; also usable by an admin.
/// </summary>
public class LogoutAllUserSessionsCommand : IRequest
{
    public Guid UserId { get; set; }
}

public class LogoutAllUserSessionsCommandValidator : AbstractValidator<LogoutAllUserSessionsCommand>
{
    public LogoutAllUserSessionsCommandValidator() => RuleFor(x => x.UserId).NotNull().NotEmpty();
}

public class LogoutAllUserSessionsCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IRequestHandler<LogoutAllUserSessionsCommand>
{
    public Task Handle(LogoutAllUserSessionsCommand request, CancellationToken cancellationToken) =>
        unitOfWork.RevokeAllUserSessionsAsync(sessionCache, request.UserId, cancellationToken);
}
