using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Clients.Queries.GetClientSessionsList;

public class GetClientSessionsListQuery : GetBaseListQuery, IRequest<PagedListDto<ClientSessionDto>>
{
    public Guid? Id { get; set; }
    public Guid ClientId { get; set; }
    public bool? IsRevoked { get; set; }
    public bool? IsExpired  { get; set; }
}

public class GetClientSessionsListQueryValidator : AbstractValidator<GetClientSessionsListQuery>
{
    public GetClientSessionsListQueryValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
    }
}

public class GetClientSessionsListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetClientSessionsListQuery, PagedListDto<ClientSessionDto>>
{
    public async Task<PagedListDto<ClientSessionDto>> Handle(GetClientSessionsListQuery request, CancellationToken cancellationToken)
    {
        var countSpec = new ClientSessionSpecification(
            request.Id,
            request.ClientId,
            request.IsRevoked,
            request.IsExpired);
        countSpec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        
        var totalCount = await unitOfWork
            .ClientSessionRepository
            .CountAsync(
                countSpec,
                cancellationToken);
        
        var spec = new ClientSessionSpecification(
            request.Id, 
            request.ClientId,
            request.IsRevoked,
            request.IsExpired, 
            request.CurrentPage,
            request.PageSize,
            request.OrderBy);
        spec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        
        var entities = await unitOfWork
            .ClientSessionRepository
            .ListAsync(spec, cancellationToken);

        return new PagedListDto<ClientSessionDto>
        {
            Items = entities.Adapt<List<ClientSessionDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}

public class ClientSessionDto : AuditableDto
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public bool IsRevoked { get;  set; }
    public DateTime? RevokedAt { get;  set; }
    public DateTime ExpiresAt { get; set; }
}