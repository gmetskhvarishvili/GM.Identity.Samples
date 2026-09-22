using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.ConsentDocuments.Commands.DeleteConsentDocument;

public class DeleteConsentDocumentCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteConsentDocumentCommandValidator : AbstractValidator<DeleteConsentDocumentCommand>
{
    public DeleteConsentDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class DeleteConsentDocumentCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteConsentDocumentCommand>
{
    public async Task Handle(DeleteConsentDocumentCommand request, CancellationToken cancellationToken)
    {
        var entity = await unitOfWork.ConsentDocumentRepository
            .FirstOrDefaultAsync(x => x.Id == request.Id
                                      && x.IsActive
                                      && !x.IsDeleted
                                      && !x.IsHidden,
                true,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.ConsentDocument,
                StringResource.Id,
                request.Id);
        }

        entity.SoftRemove();

        unitOfWork.ConsentDocumentRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
