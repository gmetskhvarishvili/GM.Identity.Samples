using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ConsentDocumentAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;
using ValidationException = GM.Exceptions.ValidationException;

namespace GM.Identity.Sample.Application.ConsentDocuments.Commands.AddConsentDocumentVersion;

/// <summary>
/// Publishes a new version of an existing consent document type: adds a new immutable row marked current and
/// supersedes the previous current version (whose text is retained). Every user's prior acceptance becomes
/// outstanding again, because pending is computed against the current version.
/// </summary>
public class AddConsentDocumentVersionCommand : IRequest<Guid>
{
    public string? ConsentType { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Version { get; set; }
    public bool IsMandatory { get; set; }
}

public class AddConsentDocumentVersionCommandValidator : AbstractValidator<AddConsentDocumentVersionCommand>
{
    public AddConsentDocumentVersionCommandValidator()
    {
        RuleFor(x => x.ConsentType).NotNull().NotEmpty();
        RuleFor(x => x.Title).NotNull().NotEmpty();
        RuleFor(x => x.Content).NotNull().NotEmpty();
        RuleFor(x => x.Version).NotNull().NotEmpty();
    }
}

public class AddConsentDocumentVersionCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<AddConsentDocumentVersionCommand, Guid>
{
    public async Task<Guid> Handle(AddConsentDocumentVersionCommand request, CancellationToken cancellationToken)
    {
        var current = await unitOfWork.ConsentDocumentRepository
            .FirstOrDefaultAsync(x => x.ConsentType == request.ConsentType
                                      && x.IsCurrent
                                      && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);

        if (current == null)
        {
            throw new NotFoundException(
                StringResource.ConsentDocument,
                StringResource.ConsentType,
                request.ConsentType!);
        }

        if (await unitOfWork.ConsentDocumentRepository.ExistsAsync(
                x => x.ConsentType == request.ConsentType
                     && x.Version == request.Version
                     && !x.IsDeleted,
                cancellationToken))
        {
            throw new ValidationException(
                $"Version '{request.Version}' of consent document '{request.ConsentType}' already exists.");
        }

        current.Supersede();
        unitOfWork.ConsentDocumentRepository.Update(current);

        var entity = ConsentDocument.Create(
            request.ConsentType!, request.Title!, request.Content!, request.Version!, request.IsMandatory);
        await unitOfWork.ConsentDocumentRepository.AddAsync(entity, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
