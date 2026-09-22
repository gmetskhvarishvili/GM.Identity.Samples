using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.Events.Users;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.Extensions.Options;
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
    ISessionCache sessionCache,
    IOptions<PasswordPolicyOptions> passwordPolicy,
    IBreachedPasswordChecker breachedPasswordChecker) : IRequestHandler<ChangeCurrentUserPasswordCommand>
{
    public async Task Handle(ChangeCurrentUserPasswordCommand request, CancellationToken cancellationToken)
    {
        PasswordPolicy.Validate(request.NewPassword, passwordPolicy.Value);
        await PasswordPolicy.EnsureNotBreachedAsync(request.NewPassword, breachedPasswordChecker, cancellationToken);

        var user = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);

        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        var (hash, salt) = PasswordHasher.Hash(request.NewPassword);
        user.ChangePassword(hash, salt);

        unitOfWork.UserRepository.Update(user);
        await unitOfWork.QueueSecurityAlertAsync(
            user.Id, user.Email, user.PhoneNumber, SecurityAlertTypes.PasswordChanged, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.RevokeAllUserSessionsAsync(sessionCache, user.Id, cancellationToken);
    }
}
