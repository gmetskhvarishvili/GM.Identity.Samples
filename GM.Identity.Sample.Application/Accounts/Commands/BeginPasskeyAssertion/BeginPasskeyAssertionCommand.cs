using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common.Security;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Commands.BeginPasskeyAssertion;

/// <summary>
/// Starts a passkey login: issues a fresh single-use challenge (base64url) the authenticator must sign. The
/// caller passes it as the WebAuthn challenge and completes login via the assertion endpoint.
/// </summary>
public class BeginPasskeyAssertionCommand : IRequest<string>
{
    public string UserName { get; set; } = null!;
}

public class BeginPasskeyAssertionCommandValidator : AbstractValidator<BeginPasskeyAssertionCommand>
{
    public BeginPasskeyAssertionCommandValidator() => RuleFor(x => x.UserName).NotNull().NotEmpty();
}

public class BeginPasskeyAssertionCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<BeginPasskeyAssertionCommand, string>
{
    public async Task<string> Handle(BeginPasskeyAssertionCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserName == request.UserName && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.UserName, request.UserName);

        var challenge = WebAuthnAssertion.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        await unitOfWork.PasskeyChallengeRepository.AddAsync(
            PasskeyChallenge.Create(user.Id, challenge, DateTime.UtcNow.AddMinutes(5)), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return challenge;
    }
}
