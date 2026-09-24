using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.SetUserTwoFactor;

/// <summary>
/// Enables or disables a second-factor method for a user in one operation (no OTP or confirmation). Enabling
/// enrols and activates the method immediately so it gates login; disabling removes the enrolment. Both are
/// idempotent.
/// </summary>
public class SetUserTwoFactorCommand : IRequest
{
    public Guid UserId { get; set; }

    /// <summary>The 2FA method to enable/disable (see the TwoFactorAuthType reference data).</summary>
    public int TwoFactorAuthTypeId { get; set; }

    /// <summary><c>true</c> to enrol and activate the method, <c>false</c> to remove it.</summary>
    public bool Enabled { get; set; }
}

public class SetUserTwoFactorCommandValidator : AbstractValidator<SetUserTwoFactorCommand>
{
    public SetUserTwoFactorCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.TwoFactorAuthTypeId).GreaterThan(0);
    }
}

public class SetUserTwoFactorCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<SetUserTwoFactorCommand>
{
    public async Task Handle(SetUserTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var userExists = await unitOfWork.UserRepository.ExistsAsync(
            x => x.Id == request.UserId && !x.IsDeleted, cancellationToken);
        if (!userExists)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        var existing = await unitOfWork.UserTwoFactorAuthTypeRepository
            .FirstOrDefaultAsync(
                x => x.UserId == request.UserId
                     && x.TwoFactorAuthTypeId == request.TwoFactorAuthTypeId
                     && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);

        if (request.Enabled)
        {
            // Already enrolled and active → nothing to do.
            if (existing is { IsConfirmed: true })
                return;

            var typeExists = await unitOfWork.TwoFactorAuthTypeRepository.ExistsAsync(
                x => x.Id == request.TwoFactorAuthTypeId && x.IsActive && !x.IsDeleted && !x.IsHidden,
                cancellationToken);
            if (!typeExists)
                throw new NotFoundException(StringResource.TwoFactorAuthType, StringResource.Id, request.TwoFactorAuthTypeId);

            if (existing == null)
            {
                var entity = UserTwoFactorAuthType.Create(request.UserId, request.TwoFactorAuthTypeId);
                entity.Confirm();
                await unitOfWork.UserTwoFactorAuthTypeRepository.AddAsync(entity, cancellationToken);
            }
            else
            {
                existing.Confirm();
                unitOfWork.UserTwoFactorAuthTypeRepository.Update(existing);
            }
        }
        else
        {
            // Idempotent: nothing enrolled → nothing to remove.
            if (existing == null)
                return;

            unitOfWork.UserTwoFactorAuthTypeRepository.Remove(existing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
