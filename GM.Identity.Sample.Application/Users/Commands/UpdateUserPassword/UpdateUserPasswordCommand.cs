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

using System;
using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Users.Commands.UpdateUserPassword;

public class UpdateUserPasswordCommand : IRequest
{
    public Guid Id { get; set; }
    public string Password { get; set; } = null!;
}

public class UpdateUserPasswordCommandValidator : AbstractValidator<UpdateUserPasswordCommand>
{
    public UpdateUserPasswordCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
        RuleFor(x => x.Password).StrongPassword();
    }
}

public class UpdateUserPasswordCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache,
    IOptions<PasswordPolicyOptions> passwordPolicy,
    IBreachedPasswordChecker breachedPasswordChecker)
    : IRequestHandler<UpdateUserPasswordCommand>
{
    public async Task Handle(UpdateUserPasswordCommand request, CancellationToken cancellationToken)
    {
        PasswordPolicy.Validate(request.Password, passwordPolicy.Value);
        await PasswordPolicy.EnsureNotBreachedAsync(request.Password, breachedPasswordChecker, cancellationToken);

        // the root aggregate
        var entity = await unitOfWork.UserRepository
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
                StringResource.User,
                StringResource.Id,
                request.Id);
        }

        var (hash, salt) = PasswordHasher
            .Hash(request.Password);

        entity.ChangePassword(hash, salt);

        // Persist the aggregate
        unitOfWork.UserRepository.Update(entity);
        await unitOfWork.QueueSecurityAlertAsync(
            entity.Id, entity.Email, entity.PhoneNumber, SecurityAlertTypes.PasswordChanged, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // A password change invalidates every existing session — force re-authentication everywhere.
        await unitOfWork.RevokeAllUserSessionsAsync(sessionCache, entity.Id, cancellationToken);
    }
}
