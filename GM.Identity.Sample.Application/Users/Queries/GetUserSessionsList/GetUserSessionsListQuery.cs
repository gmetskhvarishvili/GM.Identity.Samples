using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Users.Queries.GetUserSessionsList;

public class GetUserSessionsListQuery : GetBaseListQuery, IRequest<PagedListDto<UserSessionDto>>
{
    public Guid? Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ClientId { get; set; }
    public bool? IsRevoked { get; set; }
    public bool? IsExpired  { get; set; }
}

public class GetUserSessionsListQueryValidator : AbstractValidator<GetUserSessionsListQuery>
{
    public GetUserSessionsListQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GetUserSessionsListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUserSessionsListQuery, PagedListDto<UserSessionDto>>
{
    public async Task<PagedListDto<UserSessionDto>> Handle(GetUserSessionsListQuery request, CancellationToken cancellationToken)
    {
        var countSpec = new UserSessionSpecification(
            request.Id,
            request.UserId,
            request.ClientId,
            request.IsRevoked,
            request.IsExpired);
        
        var totalCount = await unitOfWork
            .UserSessionRepository
            .CountAsync(
                countSpec,
                cancellationToken);
        
        var spec = new UserSessionSpecification(
            request.Id, 
            request.UserId,
            request.ClientId,
            request.IsRevoked,
            request.IsExpired, 
            request.CurrentPage,
            request.PageSize,
            request.OrderBy);
        
        var entities = await unitOfWork
            .UserSessionRepository
            .ListAsync(spec, cancellationToken);

        return new PagedListDto<UserSessionDto>
        {
            Items = entities.Adapt<List<UserSessionDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}

public class UserSessionDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ClientId { get; set; }
    public bool IsRevoked { get;  set; }
    public DateTime? RevokedAt { get;  set; }
    public DateTime ExpiresAt { get; set; }
}