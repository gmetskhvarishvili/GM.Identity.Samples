using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.DisableUserTwoFactor;

/// <summary>
/// Removes a second-factor enrolment from a user. When the user's last method is removed, the password grant
/// mints a session directly again (no challenge). Idempotent — a method the user is not enrolled in is a no-op.
/// </summary>
public class DisableUserTwoFactorCommand : IRequest
{
    public Guid UserId { get; set; }

    /// <summary>The enrolled 2FA method to remove.</summary>
    public int TwoFactorAuthTypeId { get; set; }
}

public class DisableUserTwoFactorCommandValidator : AbstractValidator<DisableUserTwoFactorCommand>
{
    public DisableUserTwoFactorCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.TwoFactorAuthTypeId).GreaterThan(0);
    }
}

public class DisableUserTwoFactorCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<DisableUserTwoFactorCommand>
{
    public async Task Handle(DisableUserTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var entity = await unitOfWork.UserTwoFactorAuthTypeRepository
            .FirstOrDefaultAsync(
                x => x.UserId == request.UserId
                     && x.TwoFactorAuthTypeId == request.TwoFactorAuthTypeId
                     && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true,
                null,
                cancellationToken);

        // Idempotent: nothing enrolled → nothing to remove.
        if (entity == null)
            return;

        unitOfWork.UserTwoFactorAuthTypeRepository.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
