using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common.Security;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.RegisterPasskey;

/// <summary>
/// Registers a WebAuthn passkey for a user: stores the credential id and its ES256 public key (a
/// SubjectPublicKeyInfo blob, base64). The private key stays on the authenticator.
/// </summary>
public class RegisterPasskeyCommand : IRequest<Guid>
{
    public Guid UserId { get; set; }
    public string CredentialId { get; set; } = null!;
    public string PublicKeySpkiBase64 { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class RegisterPasskeyCommandValidator : AbstractValidator<RegisterPasskeyCommand>
{
    public RegisterPasskeyCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.CredentialId).NotNull().NotEmpty();
        RuleFor(x => x.PublicKeySpkiBase64).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}

public class RegisterPasskeyCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<RegisterPasskeyCommand, Guid>
{
    public async Task<Guid> Handle(RegisterPasskeyCommand request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.UserRepository.ExistsAsync(
                x => x.Id == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        if (await unitOfWork.UserPasskeyRepository.ExistsAsync(
                x => x.CredentialId == request.CredentialId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
            throw new AlreadyExistsException(StringResource.User, StringResource.Id, request.CredentialId);

        byte[] spki;
        try { spki = Convert.FromBase64String(request.PublicKeySpkiBase64); }
        catch { throw new ValidationException("PublicKeySpkiBase64 is not valid base64."); }

        var passkey = UserPasskey.Create(request.UserId, request.CredentialId, spki, request.Name);
        await unitOfWork.UserPasskeyRepository.AddAsync(passkey, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return passkey.Id;
    }
}
