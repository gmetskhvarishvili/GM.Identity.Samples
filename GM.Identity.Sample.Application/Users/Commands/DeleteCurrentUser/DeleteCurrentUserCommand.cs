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

namespace GM.Identity.Sample.Application.Users.Commands.DeleteCurrentUser;

/// <summary>
/// Self-service account closure: soft-removes the caller's own account and revokes all their sessions so
/// access stops immediately. The row is retained (soft delete) for audit/compliance, not hard-deleted.
/// </summary>
public class DeleteCurrentUserCommand : IRequest
{
    public Guid UserId { get; set; }
}

public class DeleteCurrentUserCommandValidator : AbstractValidator<DeleteCurrentUserCommand>
{
    public DeleteCurrentUserCommandValidator() => RuleFor(x => x.UserId).NotNull().NotEmpty();
}

public class DeleteCurrentUserCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IRequestHandler<DeleteCurrentUserCommand>
{
    public async Task Handle(DeleteCurrentUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted, true, null, cancellationToken);

        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        user.SoftRemove();
        unitOfWork.UserRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.RevokeAllUserSessionsAsync(sessionCache, user.Id, cancellationToken);
    }
}
