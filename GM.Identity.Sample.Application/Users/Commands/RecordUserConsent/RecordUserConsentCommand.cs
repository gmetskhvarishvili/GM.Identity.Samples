using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserConsentAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;
using ValidationException = GM.Exceptions.ValidationException;

namespace GM.Identity.Sample.Application.Users.Commands.RecordUserConsent;

/// <summary>Records that a user accepted a consent document at a specific version (compliance audit trail).</summary>
public class RecordUserConsentCommand : IRequest
{
    public Guid UserId { get; set; }
    public string ConsentType { get; set; } = null!;
    public string DocumentVersion { get; set; } = null!;
}

public class RecordUserConsentCommandValidator : AbstractValidator<RecordUserConsentCommand>
{
    public RecordUserConsentCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.ConsentType).NotNull().NotEmpty();
        RuleFor(x => x.DocumentVersion).NotNull().NotEmpty();
    }
}

public class RecordUserConsentCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<RecordUserConsentCommand>
{
    public async Task Handle(RecordUserConsentCommand request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.UserRepository.ExistsAsync(
                x => x.Id == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        // The document (type + version) must exist in the registry, so a client can't record acceptance of an
        // unknown document or a stale version that would never clear the pending-consent gate.
        var document = await unitOfWork.ConsentDocumentRepository
            .FirstOrDefaultAsync(x => x.ConsentType == request.ConsentType
                                      && x.IsActive && !x.IsDeleted && !x.IsHidden,
                false, null, cancellationToken);

        if (document == null)
            throw new NotFoundException(StringResource.ConsentDocument, StringResource.ConsentType, request.ConsentType);

        if (document.CurrentVersion != request.DocumentVersion)
            throw new ValidationException(
                $"Consent '{request.ConsentType}' must be accepted at the current version '{document.CurrentVersion}'.");

        await unitOfWork.UserConsentRepository.AddAsync(
            UserConsent.Create(request.UserId, request.ConsentType, request.DocumentVersion), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
