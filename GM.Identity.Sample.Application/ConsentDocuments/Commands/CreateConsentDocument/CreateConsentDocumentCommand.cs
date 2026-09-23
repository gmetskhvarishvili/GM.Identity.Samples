using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ConsentDocumentAggregate;
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
        // Reject creating a document type that already exists — publish a new version instead (AddVersion).
        if (await unitOfWork.ConsentDocumentRepository.ExistsAsync(
                x => x.ConsentType == request.ConsentType
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.ConsentDocument,
                StringResource.ConsentType,
                request.ConsentType!);
        }

        var entity = ConsentDocument.Create(
            request.ConsentType!, request.Title!, request.Content!, request.Version!, request.IsMandatory);

        await unitOfWork.ConsentDocumentRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
