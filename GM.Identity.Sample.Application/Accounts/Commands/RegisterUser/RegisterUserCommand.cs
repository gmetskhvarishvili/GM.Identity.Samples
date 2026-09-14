using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.Enums;
using GM.Identity.Sample.Domain.Events.Users;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.Extensions.Options;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Commands.RegisterUser;

/// <summary>
/// Public self-registration: creates a new (unconfirmed) user and kicks off email confirmation by issuing a
/// one-time code to the address. The account exists immediately; the user confirms their email via the
/// existing confirm flow. Enforces the configurable password policy and seeds password history.
/// </summary>
public class RegisterUserCommand : IRequest<Guid>
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = null!;
}

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
        RuleFor(x => x.Username).NotNull().NotEmpty();
        RuleFor(x => x.Password).StrongPassword();
    }
}

public class RegisterUserCommandHandler(
    IUnitOfWork unitOfWork,
    IOptions<PasswordPolicyOptions> passwordPolicy) : IRequestHandler<RegisterUserCommand, Guid>
{
    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        PasswordPolicy.Validate(request.Password, passwordPolicy.Value);

        if (await unitOfWork.UserRepository.ExistsAsync(
                x => x.Email == request.Email && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
        {
            throw new AlreadyExistsException(StringResource.User, StringResource.Email, request.Email);
        }

        var user = User.Create(request.Username, request.Email, request.PhoneNumber);
        var (hash, salt) = PasswordHasher.Hash(request.Password);
        user.UpdatePassword(hash, salt);

        await unitOfWork.UserRepository.AddAsync(user, cancellationToken);
        await unitOfWork.RecordAsync(user.Id, hash, salt, cancellationToken);

        // Announce the registration and kick off email confirmation (one-time code to the address).
        await unitOfWork.OutboxMessageRepository.AddAsync(
            OutboxMessage.From(user.Id,
                new UserRegisteredIntegrationEvent(request.Email, request.Username, request.PhoneNumber) { UserId = user.Id }),
            cancellationToken);
        await unitOfWork.OutboxMessageRepository.AddAsync(
            OutboxMessage.From(user.Id,
                new UserConfirmationInitiatedIntegrationEvent(request.Email, (int)ConfirmationType.Email) { UserId = user.Id }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
