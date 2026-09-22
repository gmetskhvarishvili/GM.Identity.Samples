using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.ConsentDocuments.Commands.UpdateConsentDocument;

public class UpdateConsentDocumentCommand : IRequest
{
    public Guid? Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? CurrentVersion { get; set; }
    public bool IsMandatory { get; set; }
}

public class UpdateConsentDocumentCommandValidator : AbstractValidator<UpdateConsentDocumentCommand>
{
    public UpdateConsentDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
        RuleFor(x => x.Title).NotNull().NotEmpty();
        RuleFor(x => x.Content).NotNull().NotEmpty();
        RuleFor(x => x.CurrentVersion).NotNull().NotEmpty();
    }
}

public class UpdateConsentDocumentCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateConsentDocumentCommand>
{
    public async Task Handle(UpdateConsentDocumentCommand request, CancellationToken cancellationToken)
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
                request.Id!);
        }

        entity.Update(request.Title!, request.Content!, request.CurrentVersion!, request.IsMandatory);

        unitOfWork.ConsentDocumentRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
