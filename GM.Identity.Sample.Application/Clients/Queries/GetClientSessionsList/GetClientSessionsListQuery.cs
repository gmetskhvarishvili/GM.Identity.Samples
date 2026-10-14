using FluentValidation;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        var dateRange = request.ToAuditDateRange();
        var paging = request.ToPagingOptions();

        var countSpec = new ClientSessionSpecification(
            request.Id,
            request.ClientId,
            request.IsRevoked,
            request.IsExpired,
            dateRange, PagingOptions.None, OrderingOptions.None, request.Visibility);
        long? totalCount = paging.IncludeTotalCount
            ? await unitOfWork.ClientSessionRepository.CountAsync(countSpec, cancellationToken)
            : null;

        var spec = new ClientSessionSpecification(
            request.Id,
            request.ClientId,
            request.IsRevoked,
            request.IsExpired,
            dateRange, paging, request.ToOrderingOptions(), request.Visibility);
        var entities = await unitOfWork.ClientSessionRepository.ListAsync(spec, cancellationToken);

        return request.ToPagedListDto(entities, entities.Adapt<List<ClientSessionDto>>(), totalCount);
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
