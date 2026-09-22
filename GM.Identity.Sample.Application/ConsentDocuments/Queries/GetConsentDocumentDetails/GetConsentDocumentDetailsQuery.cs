using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.ConsentDocuments.Queries.GetConsentDocumentDetails;

public class GetConsentDocumentDetailsQuery : IRequest<ConsentDocumentDetailsDto>
{
    public Guid? Id { get; set; }
}

public class GetConsentDocumentDetailsQueryValidator : AbstractValidator<GetConsentDocumentDetailsQuery>
{
    public GetConsentDocumentDetailsQueryValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class GetConsentDocumentDetailsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetConsentDocumentDetailsQuery, ConsentDocumentDetailsDto>
{
    public async Task<ConsentDocumentDetailsDto> Handle(
        GetConsentDocumentDetailsQuery request, CancellationToken cancellationToken)
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

        return entity.Adapt<ConsentDocumentDetailsDto>();
    }
}

public class ConsentDocumentDetailsDto : AuditableDto
{
    public Guid Id { get; set; }
    public string? ConsentType { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? CurrentVersion { get; set; }
    public bool IsMandatory { get; set; }
}
