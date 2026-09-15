using FluentValidation;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.RevokeApiKey;

/// <summary>Revokes one of a user's API keys. Idempotent — a no-op if the key doesn't exist for the user.</summary>
public class RevokeApiKeyCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid ApiKeyId { get; set; }
}

public class RevokeApiKeyCommandValidator : AbstractValidator<RevokeApiKeyCommand>
{
    public RevokeApiKeyCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.ApiKeyId).NotNull().NotEmpty();
    }
}

public class RevokeApiKeyCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<RevokeApiKeyCommand>
{
    public async Task Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        var entity = await unitOfWork.ApiKeyRepository
            .FirstOrDefaultAsync(
                x => x.Id == request.ApiKeyId && x.UserId == request.UserId
                     && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);
        if (entity == null)
            return;

        unitOfWork.ApiKeyRepository.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
