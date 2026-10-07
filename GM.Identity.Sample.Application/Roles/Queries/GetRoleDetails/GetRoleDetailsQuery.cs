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

namespace GM.Identity.Sample.Application.Roles.Queries.GetRoleDetails;

public class GetRoleDetailsQuery : GetBaseDetailsQuery, IRequest<RoleDetailsDto>
{
}

public class GetRoleDetailsQueryValidator : AbstractValidator<GetRoleDetailsQuery>
{
    public GetRoleDetailsQueryValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class GetRoleDetailsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetRoleDetailsQuery, RoleDetailsDto>
{
    public async Task<RoleDetailsDto> Handle(GetRoleDetailsQuery request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.RoleRepository
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
                StringResource.Role,
                StringResource.Id,
                request.Id!);
        }

        var result = entity.Adapt<RoleDetailsDto>();

        return result;
    }
}

public class RoleDetailsDto : AuditableDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}
