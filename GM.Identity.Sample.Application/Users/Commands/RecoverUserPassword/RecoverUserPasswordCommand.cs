using FluentValidation;
using GM.Exceptions;
using GM.Identity;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Application.Infrastructure.Services.OTP;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Users.Commands.RecoverUserPassword;

public class RecoverUserPasswordCommand : IRequest
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class RecoverUserPasswordCommandValidator : AbstractValidator<RecoverUserPasswordCommand>
{
    public RecoverUserPasswordCommandValidator(
        IPasswordPolicyOptions passwordPolicy, IBreachedPasswordChecker breachedPasswordChecker)
    {
        RuleFor(x => x.Email).NotNull().NotEmpty();
        RuleFor(x => x.Code).NotNull().NotEmpty();
        RuleFor(x => x.Password).StrongPassword(passwordPolicy);
        RuleFor(x => x.Password).NotBreached(breachedPasswordChecker);
    }
}

public class RecoverUserPasswordCommandHandler(
    IUnitOfWork unitOfWork,
    IOTPService otpService) : IRequestHandler<RecoverUserPasswordCommand>
{
    public async Task Handle(RecoverUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var entity = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Email == request.Email
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
                StringResource.Email,
                request.Email);
        }

        // Validate the reset code sent to the user's email (same subject + purpose GM.OTP issued it against in
        // ResetUserPassword). Throws if the code is wrong or expired.
        await otpService.VerifyOTP(
            new VerifyOTPDto { Subject = entity.Email!, Purpose = OtpPurpose.ResetPassword, Code = request.Code },
            cancellationToken);

        // Set the new password and, since a reset invalidates prior access, revoke the user's sessions (which also
        // queues a SessionsRevoked outbox message for cache eviction) — all committed in one save.
        var (hash, salt) = PasswordHasher.Hash(request.Password);
        entity.ChangePassword(hash, salt);

        unitOfWork.UserRepository.Update(entity);
        await unitOfWork.UserSessionRepository.RevokeAllForUserAsync(entity.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
