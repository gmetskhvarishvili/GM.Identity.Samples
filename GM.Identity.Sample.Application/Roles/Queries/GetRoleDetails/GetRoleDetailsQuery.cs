using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Roles.Queries.GetRoleDetails;

public class GetRoleDetailsQuery : IRequest<RoleDetailsDto>
{
    public Guid? Id { get; set; }
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
                                      && x.IsActive
                                      && !x.IsDeleted
                                      && !x.IsHidden,
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

public class RoleDetailsDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}