using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Clients.Commands.RotateClientSecret;

/// <summary>
/// Rotates a client's secret: generates a fresh secret, stores only its hash, and revokes the client's active
/// sessions so tokens minted under the old secret stop working immediately. Returns the new plaintext secret
/// exactly once — it is never recoverable afterwards.
/// </summary>
public class RotateClientSecretCommand : IRequest<string>
{
    public Guid ClientId { get; set; }
}

public class RotateClientSecretCommandValidator : AbstractValidator<RotateClientSecretCommand>
{
    public RotateClientSecretCommandValidator() => RuleFor(x => x.ClientId).NotNull().NotEmpty();
}

public class RotateClientSecretCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IRequestHandler<RotateClientSecretCommand, string>
{
    public async Task<string> Handle(RotateClientSecretCommand request, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientRepository
            .FirstOrDefaultAsync(x => x.Id == request.ClientId && !x.IsDeleted, true, null, cancellationToken);

        if (client == null)
            throw new NotFoundException(StringResource.Client, StringResource.Id, request.ClientId);

        var newSecret = TokenGenerator.Generate();
        var (hash, salt) = PasswordHasher.Hash(newSecret);
        client.UpdateSecret(hash, salt);

        unitOfWork.ClientRepository.Update(client);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // The old secret is void — kill any sessions it still backs.
        await unitOfWork.RevokeAllClientSessionsAsync(sessionCache, client.Id, cancellationToken);

        return newSecret;
    }
}
