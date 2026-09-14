using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;
using ValidationException = GM.Exceptions.ValidationException;

using System.Threading;
using System.Threading.Tasks;
using System;

namespace GM.Identity.Sample.Application.Accounts.Commands.RevokeToken;

/// <summary>
/// Revokes an opaque access or refresh token (OAuth token revocation, RFC 7009). Authenticates the client,
/// then revokes the matching session (user or client) and evicts it from the session cache. Idempotent —
/// an unknown token is a no-op.
/// </summary>
public class RevokeTokenCommand : IRequest
{
    public Guid ClientId { get; set; }
    public string ClientSecret { get; set; } = null!;
    public string Token { get; set; } = null!;
}

public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ClientSecret).NotNull().NotEmpty();
        RuleFor(x => x.Token).NotNull().NotEmpty();
    }
}

public class RevokeTokenCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IRequestHandler<RevokeTokenCommand>
{
    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.ClientId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);

        if (client == null)
            throw new NotFoundException(StringResource.Client, StringResource.Id, request.ClientId);
        if (!PasswordHasher.Verify(request.ClientSecret, client.SecretHash, client.SecretSalt))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        var hash = TokenGenerator.Hash(request.Token);

        // A user session matches on its access-token hash or its refresh-token hash.
        var userSession = await unitOfWork.UserSessionRepository
            .Query(true, null)
            .FirstOrDefaultAsync(x => (x.TokenHash == hash || x.RefreshTokenHash == hash) && !x.IsRevoked, cancellationToken);
        if (userSession != null)
        {
            userSession.Revoke();
            unitOfWork.UserSessionRepository.Update(userSession);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await sessionCache.RemoveAsync(userSession.TokenHash, cancellationToken);
            return;
        }

        var clientSession = await unitOfWork.ClientSessionRepository
            .Query(true, null)
            .FirstOrDefaultAsync(x => x.TokenHash == hash && !x.IsRevoked, cancellationToken);
        if (clientSession != null)
        {
            clientSession.Revoke();
            unitOfWork.ClientSessionRepository.Update(clientSession);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await sessionCache.RemoveAsync(hash, cancellationToken);
        }

        // Unknown token → no-op (RFC 7009).
    }
}
