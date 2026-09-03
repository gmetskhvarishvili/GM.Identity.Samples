using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Operations.Queries.GetOperationDetails;

public class GetOperationDetailsQuery : IRequest<OperationDetailsDto>
{
    public Guid? Id { get; set; }
}

public class GetOperationDetailsQueryValidator : AbstractValidator<GetOperationDetailsQuery>
{
    public GetOperationDetailsQueryValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class GetOperationDetailsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetOperationDetailsQuery, OperationDetailsDto>
{
    public async Task<OperationDetailsDto> Handle(GetOperationDetailsQuery request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.OperationRepository
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
                StringResource.Operation,
                StringResource.Id,
                request.Id!);
        }

        var result = entity.Adapt<OperationDetailsDto>();

        return result;
    }
}

public class OperationDetailsDto : AuditableDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}