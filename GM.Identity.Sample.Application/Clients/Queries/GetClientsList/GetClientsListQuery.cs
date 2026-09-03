using FluentValidation;
using GM.API.Application.Models;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate.Specifications;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Clients.Queries.GetClientsList;

public class GetClientsListQuery: GetBaseListQuery, IRequest<PagedListDto<ClientDto>>
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
}
public class GetClientsListQueryValidator : AbstractValidator<GetClientsListQuery>;

public class GetClientsListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetClientsListQuery, PagedListDto<ClientDto>>
{
    public async Task<PagedListDto<ClientDto>> Handle(GetClientsListQuery request, CancellationToken cancellationToken)
    {
        var countSpec = new ClientSpecification(
            request.Id, 
            request.Name);
        countSpec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        
        var totalCount = await unitOfWork
            .ClientRepository
            .CountAsync(
                countSpec,
                cancellationToken);
        
        var spec = new ClientSpecification(
            request.Id, 
            request.Name, 
            request.CurrentPage, 
            request.PageSize,
            request.OrderBy);
        spec.ApplyAuditDateRangeFilter(request.CreatedAtFrom, request.CreatedAtTo, request.UpdatedAtFrom, request.UpdatedAtTo);
        var entities = await unitOfWork.ClientRepository.ListAsync(spec, cancellationToken);
        
        return new PagedListDto<ClientDto>
        {
            Items = entities.Adapt<List<ClientDto>>(),
            CurrentPage = request.CurrentPage,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}

public class ClientDto : AuditableDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}