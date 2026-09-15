using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Infrastructure.Services.OTP;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.Enums;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.ConfirmContactChange;

/// <summary>
/// Completes a verify-before-apply contact change: validates the one-time code sent to the pending new contact,
/// then writes it onto the user and marks it confirmed. Only now does the user's email/phone actually change.
/// </summary>
public class ConfirmContactChangeCommand : IRequest
{
    public Guid UserId { get; set; }
    public string Code { get; set; } = null!;
}

public class ConfirmContactChangeCommandValidator : AbstractValidator<ConfirmContactChangeCommand>
{
    public ConfirmContactChangeCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.Code).NotNull().NotEmpty();
    }
}

public class ConfirmContactChangeCommandHandler(
    IUnitOfWork unitOfWork,
    IOTPService otpService) : IRequestHandler<ConfirmContactChangeCommand>
{
    public async Task Handle(ConfirmContactChangeCommand request, CancellationToken cancellationToken)
    {
        var pending = await unitOfWork.UserPendingContactChangeRepository
            .FirstOrDefaultAsync(
                x => x.UserId == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);
        if (pending == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        var user = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(
                x => x.Id == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);
        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        // Validate the code against the NEW contact (throws if wrong/expired).
        await otpService.VerifyOTP(
            new VerifyOTPDto { Subject = pending.NewContact, Purpose = OtpPurpose.ConfirmUser, Code = request.Code },
            cancellationToken);

        // Apply the change and mark the new contact confirmed.
        if (pending.ConfirmationType.ConfirmsPhoneNumber())
        {
            user.Update(user.UserName, user.Email, pending.NewContact);
            user.ConfirmPhoneNumber();
        }
        else
        {
            user.Update(user.UserName, pending.NewContact, user.PhoneNumber);
            user.ConfirmEmail();
        }

        unitOfWork.UserRepository.Update(user);
        unitOfWork.UserPendingContactChangeRepository.Remove(pending);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
