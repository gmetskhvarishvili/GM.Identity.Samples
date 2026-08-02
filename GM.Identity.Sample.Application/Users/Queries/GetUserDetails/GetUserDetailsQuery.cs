using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Users.Queries.GetUserDetails;

public class GetUserDetailsQuery : IRequest<UserDetailsDto>
{
    public Guid Id { get; set; }
}

public class GetUserDetailsQueryValidator : AbstractValidator<GetUserDetailsQuery>
{
    public GetUserDetailsQueryValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class GetUserDetailsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetUserDetailsQuery, UserDetailsDto>
{
    public async Task<UserDetailsDto> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.UserRepository
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
                StringResource.User,
                StringResource.Id,
                request.Id);
        }

        var result = entity.Adapt<UserDetailsDto>();

        return result;
    }
}

public class UserDetailsDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
}