using FluentValidation;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        var dateRange = request.ToAuditDateRange();

        var countSpec = new UserSessionSpecification(
            request.Id,
            request.UserId,
            request.ClientId,
            request.IsRevoked,
            request.IsExpired,
            dateRange, PagingOptions.None, OrderingOptions.None);
        var totalCount = await unitOfWork.UserSessionRepository.CountAsync(countSpec, cancellationToken);

        var spec = new UserSessionSpecification(
            request.Id,
            request.UserId,
            request.ClientId,
            request.IsRevoked,
            request.IsExpired,
            dateRange, request.ToPagingOptions(), request.ToOrderingOptions());
        var entities = await unitOfWork.UserSessionRepository.ListAsync(spec, cancellationToken);

        return new PagedListDto<UserSessionDto>
        {
            Items = entities.Adapt<List<UserSessionDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}

public class UserSessionDto : AuditableDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ClientId { get; set; }
    public bool IsRevoked { get;  set; }
    public DateTime? RevokedAt { get;  set; }
    public DateTime ExpiresAt { get; set; }

    // Request/tracing context captured when the session was created (see UserSession : ICapturesActorContext).
    public Guid? TenantId { get; set; }
    public string? ChannelId { get; set; }
    public string? Culture { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? Source { get; set; }
}
