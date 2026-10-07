using Asp.Versioning;
using GM.API.Authorization;
using GM.API.Controllers;
using GM.Identity.Sample.Application.DomainEvents.Queries.GetDomainEventsList;
using GM.Identity.Sample.Domain.SeedWork;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.API.DomainEvents;

/// <summary>
/// Admin access to the domain-event log (audit trail): filter by user, client, session, tenant, ip, channel,
/// event type, and occurred/created time ranges — e.g. the full action history of a given user.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DomainEventsController : BaseController
{
    /// <summary>
    /// Get Domain Events
    /// </summary>
    /// <param name="request">Audit-trail filters (all optional).</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Matching domain-event records, newest first.</returns>
    [HasPermission(nameof(GetDomainEvents))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
    [HttpGet(Name = nameof(GetDomainEvents))]
    [ProducesResponseType(typeof(IEnumerable<DomainEventModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDomainEvents(
        [FromQuery] GetDomainEventsModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetDomainEventsListQuery>();
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<DomainEventModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }
}
