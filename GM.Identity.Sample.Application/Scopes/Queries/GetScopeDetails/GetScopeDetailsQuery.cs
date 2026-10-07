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

namespace GM.Identity.Sample.Application.Scopes.Queries.GetScopeDetails;

public class GetScopeDetailsQuery : GetBaseDetailsQuery, IRequest<ScopeDetailsDto>
{
}

public class GetScopeDetailsQueryValidator : AbstractValidator<GetScopeDetailsQuery>
{
    public GetScopeDetailsQueryValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class GetScopeDetailsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetScopeDetailsQuery, ScopeDetailsDto>
{
    public async Task<ScopeDetailsDto> Handle(GetScopeDetailsQuery request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.ScopeRepository
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
                StringResource.Scope,
                StringResource.Id,
                request.Id!);
        }

        var result = entity.Adapt<ScopeDetailsDto>();

        return result;
    }
}

public class ScopeDetailsDto : AuditableDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}
