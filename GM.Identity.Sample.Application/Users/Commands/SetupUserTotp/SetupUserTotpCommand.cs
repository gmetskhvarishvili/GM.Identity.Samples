using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common.Security;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTotpDeviceAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.SetupUserTotp;

/// <summary>
/// Begins authenticator-app (TOTP) enrolment: generates a fresh secret, stores it as a <em>pending</em> device
/// (replacing any prior one), and returns the secret plus the <c>otpauth://</c> provisioning URI to show as a
/// QR code. The device does not gate login until confirmed. Returned secret is shown once.
/// </summary>
public class SetupUserTotpCommand : IRequest<SetupUserTotpResultDto>
{
    public Guid UserId { get; set; }
}

public class SetupUserTotpCommandValidator : AbstractValidator<SetupUserTotpCommand>
{
    public SetupUserTotpCommandValidator() => RuleFor(x => x.UserId).NotNull().NotEmpty();
}

public class SetupUserTotpCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SetupUserTotpCommand, SetupUserTotpResultDto>
{
    private const string Issuer = "GM Identity Sample";

    public async Task<SetupUserTotpResultDto> Handle(SetupUserTotpCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted, false, null, cancellationToken);
        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        // Replace any existing device — re-running setup starts a fresh secret.
        var existing = await unitOfWork.UserTotpDeviceRepository
            .Query(true, null)
            .Where(x => x.UserId == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
            unitOfWork.UserTotpDeviceRepository.RemoveRange(existing);

        var secret = Totp.GenerateSecret();
        await unitOfWork.UserTotpDeviceRepository.AddAsync(
            UserTotpDevice.Create(request.UserId, secret), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accountName = !string.IsNullOrWhiteSpace(user.Email) ? user.Email : user.UserName;
        return new SetupUserTotpResultDto(secret, Totp.BuildProvisioningUri(secret, Issuer, accountName!));
    }
}

public record SetupUserTotpResultDto(string Secret, string OtpauthUri);
