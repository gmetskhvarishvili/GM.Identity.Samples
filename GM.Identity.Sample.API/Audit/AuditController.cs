using Asp.Versioning;
using GM.API.Authorization;
using GM.API.Controllers;
using GM.Identity.Sample.Application.Audit.Queries.SearchAuditTrail;
using GM.Identity.Sample.Domain.SeedWork;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.API.Audit;

/// <summary>
/// Audit Controller — global, filterable search over the durable domain-event log across every aggregate.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuditController : BaseController
{
    /// <summary>Search the audit log by aggregate type, acting user, event type, and/or date range.</summary>
    [HasPermission(nameof(SearchAuditTrail))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
    [HttpGet(Name = nameof(SearchAuditTrail))]
    [ProducesResponseType(typeof(IEnumerable<AuditEntryModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAuditTrail(
        [FromQuery] SearchAuditTrailModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<SearchAuditTrailQuery>();
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<AuditEntryModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }
}
