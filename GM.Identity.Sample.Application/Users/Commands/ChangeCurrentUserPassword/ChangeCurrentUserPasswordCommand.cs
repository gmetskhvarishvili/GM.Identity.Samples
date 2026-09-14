using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.ChangeCurrentUserPassword;

/// <summary>Self-service password change: verifies the caller's current password before setting a new one,
/// then revokes all their sessions (including this one) so every device re-authenticates.</summary>
public class ChangeCurrentUserPasswordCommand : IRequest
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

public class ChangeCurrentUserPasswordCommandValidator : AbstractValidator<ChangeCurrentUserPasswordCommand>
{
    public ChangeCurrentUserPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.CurrentPassword).NotNull().NotEmpty();
        RuleFor(x => x.NewPassword).StrongPassword();
    }
}

public class ChangeCurrentUserPasswordCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IRequestHandler<ChangeCurrentUserPasswordCommand>
{
    public async Task Handle(ChangeCurrentUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);

        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        // Reject reuse of the current or a recent password.
        await unitOfWork.EnsureNotReusedAsync(
            user.Id, request.NewPassword, user.PasswordHash, user.PasswordSalt, cancellationToken);

        var (hash, salt) = PasswordHasher.Hash(request.NewPassword);
        user.UpdatePassword(hash, salt);

        unitOfWork.UserRepository.Update(user);
        await unitOfWork.RecordAsync(user.Id, hash, salt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.RevokeAllUserSessionsAsync(sessionCache, user.Id, cancellationToken);
    }
}
