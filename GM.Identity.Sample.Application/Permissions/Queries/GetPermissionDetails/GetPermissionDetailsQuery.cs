using FluentValidation;
using GM.Exceptions;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Permissions.Queries.GetPermissionDetails;

public class GetPermissionDetailsQuery : GetBaseDetailsQuery, IRequest<PermissionDetailsDto>;

public class GetPermissionDetailsQueryValidator : AbstractValidator<GetPermissionDetailsQuery>
{
    public GetPermissionDetailsQueryValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class GetPermissionDetailsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetPermissionDetailsQuery, PermissionDetailsDto>
{
    public async Task<PermissionDetailsDto> Handle(GetPermissionDetailsQuery request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.PermissionRepository
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
                StringResource.Permission,
                StringResource.Id,
                request.Id!);
        }

        var result = entity.Adapt<PermissionDetailsDto>();

        return result;
    }
}

public class PermissionDetailsDto : AuditableDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
