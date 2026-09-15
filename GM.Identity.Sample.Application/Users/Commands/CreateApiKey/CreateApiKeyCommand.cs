using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ApiKeyAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.CreateApiKey;

/// <summary>
/// Issues a personal access token (API key) for a user. Returns the plaintext key exactly once (only its hash
/// is stored). The key authenticates non-interactively via grant_type=api_key at the token endpoint.
/// </summary>
public class CreateApiKeyCommand : IRequest<string>
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public DateTime? ExpiresAt { get; set; }
}

public class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleFor(x => x.ExpiresAt).GreaterThan(_ => DateTime.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("The expiry must be in the future.");
    }
}

public class CreateApiKeyCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateApiKeyCommand, string>
{
    public async Task<string> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.UserRepository.ExistsAsync(
                x => x.Id == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        var key = TokenGenerator.Generate();
        await unitOfWork.ApiKeyRepository.AddAsync(
            ApiKey.Create(request.UserId, request.Name, TokenGenerator.Hash(key), request.ExpiresAt), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return key; // shown once
    }
}
