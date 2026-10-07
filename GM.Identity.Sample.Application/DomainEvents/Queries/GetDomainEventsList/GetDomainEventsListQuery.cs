using FluentValidation;
using GM.API.Application.Models;
using GM.EntityFramework.Domain.Specifications;
using GM.Identity.Sample.Domain.BoundedContext.AuditBoundedContext.AuditAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.DomainEvents.Queries.GetDomainEventsList;

/// <summary>
/// Queries the durable domain-event log (audit trail) with any combination of filters, so an admin can see a
/// full action history — e.g. everything a given <see cref="UserId"/> did. Paged, newest-first.
/// </summary>
public class GetDomainEventsListQuery : GetBaseListQuery, IRequest<PagedListDto<DomainEventDto>>
{
    public Guid? UserId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? TenantId { get; set; }
    public string? IpAddress { get; set; }
    public string? ChannelId { get; set; }
    public string? EventType { get; set; }
    public DateTime? OccurredFrom { get; set; }
    public DateTime? OccurredTo { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}

public class GetDomainEventsListQueryValidator : AbstractValidator<GetDomainEventsListQuery>;

public class GetDomainEventsListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetDomainEventsListQuery, PagedListDto<DomainEventDto>>
{
    public async Task<PagedListDto<DomainEventDto>> Handle(GetDomainEventsListQuery request, CancellationToken cancellationToken)
    {
        var countSpec = new DomainEventLogSpecification(
            request.UserId, request.ClientId, request.SessionId, request.TenantId,
            request.IpAddress, request.ChannelId, request.EventType,
            request.OccurredFrom, request.OccurredTo, request.CreatedFrom, request.CreatedTo,
            PagingOptions.None);
        var totalCount = await unitOfWork.DomainEventLogRepository.CountAsync(countSpec, cancellationToken);

        var spec = new DomainEventLogSpecification(
            request.UserId, request.ClientId, request.SessionId, request.TenantId,
            request.IpAddress, request.ChannelId, request.EventType,
            request.OccurredFrom, request.OccurredTo, request.CreatedFrom, request.CreatedTo,
            request.ToPagingOptions());
        var items = await unitOfWork.DomainEventLogRepository.ListAsync(spec, cancellationToken);

        return new PagedListDto<DomainEventDto>
        {
            Items = items.Adapt<List<DomainEventDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}

public class DomainEventDto
{
    public Guid Id { get; set; }
    public string? AggregateType { get; set; }
    public string? AggregateId { get; set; }
    public string? EventType { get; set; }
    public string? Payload { get; set; }

    // Rendered as strings via the shared DateTime→string Mapster config (dd/MM/yyyy HH:mm:ss).
    public string OccurredOn { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;

    public Guid? UserId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? SessionId { get; set; }
    public string? ChannelId { get; set; }
    public string? Culture { get; set; }
    public string? Source { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public string? UserAgent { get; set; }
    public string? IdempotencyKey { get; set; }
}
