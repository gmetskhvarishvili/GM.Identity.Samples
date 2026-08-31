using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Clients.Queries.GetClientDetails;

public class GetClientDetailsQuery: IRequest<ClientDetailsDto>
{
    public Guid Id { get; set; }
}

public class GetClientDetailsQueryValidator : AbstractValidator<GetClientDetailsQuery>
{
    public GetClientDetailsQueryValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class GetClientDetailsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetClientDetailsQuery, ClientDetailsDto>
{
    public async Task<ClientDetailsDto> Handle(GetClientDetailsQuery request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.ClientRepository
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
                StringResource.Client,
                StringResource.Id,
                request.Id);
        }

        var result = entity.Adapt<ClientDetailsDto>();

        return result;
    }
}

public class ClientDetailsDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}