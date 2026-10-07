using FluentValidation;
using GM.Exceptions;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Clients.Queries.GetClientDetails;

public class GetClientDetailsQuery: GetBaseDetailsQuery, IRequest<ClientDetailsDto>
{
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
                                      && (request.Visibility.HasFlag(VisibilityScope.IncludeInactive) || x.IsActive)
                                      && (request.Visibility.HasFlag(VisibilityScope.IncludeDeleted) || !x.IsDeleted)
                                      && (request.Visibility.HasFlag(VisibilityScope.IncludeHidden) || !x.IsHidden),
                true,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.Client,
                StringResource.Id,
                request.Id!);
        }

        var result = entity.Adapt<ClientDetailsDto>();

        return result;
    }
}

public class ClientDetailsDto : AuditableDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}
