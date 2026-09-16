using FluentValidation;
using GM.Exceptions;
using GM.Identity;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.DeviceCodeAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Commands.ApproveDevice;

/// <summary>
/// The user-facing half of the device grant (RFC 8628 §3.3): the user, authenticated with their credentials,
/// approves (or denies) the user_code shown on the browserless device. Approval binds the device request to the
/// user so the device's next poll receives tokens; denial stops it.
/// </summary>
public class ApproveDeviceCommand : IRequest
{
    public string UserCode { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;

    /// <summary>True to approve, false to deny.</summary>
    public bool Approve { get; set; } = true;
}

public class ApproveDeviceCommandValidator : AbstractValidator<ApproveDeviceCommand>
{
    public ApproveDeviceCommandValidator()
    {
        RuleFor(x => x.UserCode).NotNull().NotEmpty();
        RuleFor(x => x.UserName).NotNull().NotEmpty();
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}

public class ApproveDeviceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ApproveDeviceCommand>
{
    public async Task Handle(ApproveDeviceCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var user = await unitOfWork.UserRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserName == request.UserName && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (user == null || user.IsBlocked
            || (user.LockoutEnd is { } lockoutEnd && lockoutEnd > now)
            || !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new ValidationException(ExceptionsResource.InvalidCredentials);
        }

        var normalized = Normalize(request.UserCode);
        var deviceCode = await unitOfWork.DeviceCodeRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserCode == normalized && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);

        if (deviceCode == null || deviceCode.IsExpired(now) || deviceCode.Status != DeviceCodeStatus.Pending)
            throw new ValidationException("Invalid or expired user code.");

        if (request.Approve)
            deviceCode.Approve(user.Id);
        else
            deviceCode.Deny();

        unitOfWork.DeviceCodeRepository.Update(deviceCode);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // Users type the code with a dash and any case; normalize to how it is stored.
    private static string Normalize(string userCode) =>
        userCode.Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
}
