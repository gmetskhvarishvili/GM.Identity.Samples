using FluentValidation;
using GM.Exceptions;
using GM.Identity;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.ConfirmUserTotp;

/// <summary>
/// Confirms a pending authenticator-app enrolment by validating a code produced from the device's secret. Only
/// a confirmed TOTP device gates login. Idempotent — an already-confirmed device succeeds without re-checking.
/// </summary>
public class ConfirmUserTotpCommand : IRequest
{
    public Guid UserId { get; set; }
    public string Code { get; set; } = null!;
}

public class ConfirmUserTotpCommandValidator : AbstractValidator<ConfirmUserTotpCommand>
{
    public ConfirmUserTotpCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.Code).NotNull().NotEmpty();
    }
}

public class ConfirmUserTotpCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ConfirmUserTotpCommand>
{
    public async Task Handle(ConfirmUserTotpCommand request, CancellationToken cancellationToken)
    {
        var device = await unitOfWork.UserTotpDeviceRepository
            .FirstOrDefaultAsync(
                x => x.UserId == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);

        if (device == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        if (device.IsConfirmed)
            return;

        if (!Totp.Verify(device.SecretBase32, request.Code))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        device.Confirm();
        unitOfWork.UserTotpDeviceRepository.Update(device);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
