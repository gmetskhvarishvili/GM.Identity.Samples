using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Infrastructure.Services.OTP;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.ConfirmUserTwoFactor;

/// <summary>
/// Confirms a pending second-factor enrolment by validating the setup one-time code the user received. Only a
/// confirmed method gates login, so this is the step that actually activates 2FA for the user. Idempotent — if
/// the method is already confirmed, it succeeds without re-validating.
/// </summary>
public class ConfirmUserTwoFactorCommand : IRequest
{
    public Guid UserId { get; set; }
    public int TwoFactorAuthTypeId { get; set; }

    /// <summary>The one-time setup code the user received when the method was enabled.</summary>
    public string Code { get; set; } = null!;
}

public class ConfirmUserTwoFactorCommandValidator : AbstractValidator<ConfirmUserTwoFactorCommand>
{
    public ConfirmUserTwoFactorCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.TwoFactorAuthTypeId).GreaterThan(0);
        RuleFor(x => x.Code).NotNull().NotEmpty();
    }
}

public class ConfirmUserTwoFactorCommandHandler(
    IUnitOfWork unitOfWork,
    IOTPService otpService) : IRequestHandler<ConfirmUserTwoFactorCommand>
{
    public async Task Handle(ConfirmUserTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted, false, null, cancellationToken);
        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        var enrolment = await unitOfWork.UserTwoFactorAuthTypeRepository
            .FirstOrDefaultAsync(
                x => x.UserId == request.UserId
                     && x.TwoFactorAuthTypeId == request.TwoFactorAuthTypeId
                     && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);

        if (enrolment == null)
            throw new NotFoundException(StringResource.TwoFactorAuthType, StringResource.Id, request.TwoFactorAuthTypeId);

        // Already confirmed → idempotent success.
        if (enrolment.IsConfirmed)
            return;

        var subject = !string.IsNullOrWhiteSpace(user.Email) ? user.Email : user.PhoneNumber;
        if (string.IsNullOrWhiteSpace(subject))
            throw new ValidationException("This account has no contact to validate the setup code against.");

        // Throws ValidationException if the code is wrong/expired.
        await otpService.VerifyOTP(
            new VerifyOTPDto { Subject = subject, Purpose = OtpPurpose.TwoFactor, Code = request.Code },
            cancellationToken);

        enrolment.Confirm();
        unitOfWork.UserTwoFactorAuthTypeRepository.Update(enrolment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
