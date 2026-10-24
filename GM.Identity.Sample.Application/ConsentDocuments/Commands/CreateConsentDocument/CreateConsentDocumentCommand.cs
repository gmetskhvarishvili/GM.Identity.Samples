using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.ComplianceBoundedContext.ConsentDocumentAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.ConsentDocuments.Commands.CreateConsentDocument;

public class CreateConsentDocumentCommand : IRequest<Guid>
{
    public string? ConsentType { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Version { get; set; }
    public bool IsMandatory { get; set; }
}

public class CreateConsentDocumentCommandValidator : AbstractValidator<CreateConsentDocumentCommand>
{
    public CreateConsentDocumentCommandValidator()
    {
        RuleFor(x => x.ConsentType).NotNull().NotEmpty();
        RuleFor(x => x.Title).NotNull().NotEmpty();
        RuleFor(x => x.Content).NotNull().NotEmpty();
        RuleFor(x => x.Version).NotNull().NotEmpty();
    }
}

public class CreateConsentDocumentCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateConsentDocumentCommand, Guid>
{
    public async Task<Guid> Handle(CreateConsentDocumentCommand request, CancellationToken cancellationToken)
    {
        // A document is identified by type + version: the same version of a type can't be published twice
        // (current or superseded), but a new version of an existing type is fine.
        if (await unitOfWork.ConsentDocumentRepository.ExistsAsync(
                x => x.ConsentType == request.ConsentType
                     && x.Version == request.Version
                     && !x.IsDeleted,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.ConsentDocument,
                StringResource.Version,
                request.Version!);
        }

        // Publishing a new version of an existing type supersedes the current one, so exactly one version of
        // each type is current (the same as AddConsentDocumentVersion).
        var current = await unitOfWork.ConsentDocumentRepository
            .FirstOrDefaultAsync(x => x.ConsentType == request.ConsentType
                                      && x.IsCurrent
                                      && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);
        if (current != null)
        {
            current.Supersede();
            unitOfWork.ConsentDocumentRepository.Update(current);
        }

        var entity = ConsentDocument.Create(
            request.ConsentType!, request.Title!, request.Content!, request.Version!, request.IsMandatory);

        await unitOfWork.ConsentDocumentRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
