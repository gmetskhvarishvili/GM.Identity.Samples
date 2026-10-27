using FluentValidation;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.Enums;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using ValidationException = GM.Exceptions.ValidationException;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Commands.SendTwoFactorCode;

/// <summary>
/// The second step of a 2FA login: after the password grant answered with a challenge, the user picks one of
/// their enrolled delivery methods (Email, SMS or WhatsApp) and the one-time code is sent over it. The client and
/// user credentials are checked again — the challenge carries no proof of its own — so a wrong password counts
/// towards lockout here too. Calling it again sends a fresh code and invalidates the previous one.
/// </summary>
public class SendTwoFactorCodeCommand : IRequest
{
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public Guid ClientId { get; set; }
    public string ClientSecret { get; set; } = null!;

    /// <summary>The enrolled method to send the code over (a TwoFactorMethod id: 1 Email, 2 SMS, 3 WhatsApp).</summary>
    public int TwoFactorAuthTypeId { get; set; }
}

public class SendTwoFactorCodeCommandValidator : AbstractValidator<SendTwoFactorCodeCommand>
{
    public SendTwoFactorCodeCommandValidator()
    {
        RuleFor(x => x.UserName).NotNull().NotEmpty();
        RuleFor(x => x.Password).NotNull().NotEmpty();
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ClientSecret).NotNull().NotEmpty();
        RuleFor(x => x.TwoFactorAuthTypeId)
            .Must(id => Enum.IsDefined(typeof(TwoFactorMethod), id))
            .WithMessage("Choose a method that delivers a code: 1 (Email), 2 (SMS) or 3 (WhatsApp).");
    }
}

public class SendTwoFactorCodeCommandHandler(
    IUnitOfWork unitOfWork,
    IAuthOptions options) : IRequestHandler<SendTwoFactorCodeCommand>
{
    public async Task Handle(SendTwoFactorCodeCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.AuthenticateClientAsync(request.ClientId, request.ClientSecret, cancellationToken);
        var user = await unitOfWork.AuthenticateUserAsync(
            request.UserName, request.Password, DateTime.UtcNow, options, cancellationToken);

        // Only to a method the user enrolled in: the choice is among their own second factors, not any contact.
        var enrolled = await unitOfWork.GetEnrolledTwoFactorTypeIdsAsync(user.Id, cancellationToken);
        if (!enrolled.Contains(request.TwoFactorAuthTypeId))
            throw new ValidationException("This account is not enrolled in that two-factor method.");

        await unitOfWork.QueueTwoFactorCodeAsync(user, (TwoFactorMethod)request.TwoFactorAuthTypeId, cancellationToken);
    }
}
