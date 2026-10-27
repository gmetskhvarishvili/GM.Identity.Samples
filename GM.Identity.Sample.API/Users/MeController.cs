using Asp.Versioning;
using GM.API.Controllers;
using GM.API.Models;
using GM.EntityFramework.Domain.Common;
using GM.Identity.Sample.Application.Users.Queries.GetMyPermissions;
using GM.Identity.Sample.Application.Users.Queries.GetUserDetails;
using GM.Identity.Sample.Application.Users.Queries.GetUserSessionsList;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.API.Users;

/// <summary>
/// The signed-in user's own data. The user is always the one the gateway resolved from the access token
/// (forwarded as X-User-Id) — never an id the caller passes — so these need no admin permission: a caller can
/// only ever read their own profile, sessions and permissions.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class MeController : BaseController
{
    /// <summary>The signed-in user's details.</summary>
    /// <param name="currentActor"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>User Details</returns>
    [HttpGet(Name = nameof(GetMe))]
    [ProducesResponseType(typeof(UserDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(
        [FromServices] ICurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        var response = await Mediator.Send(new GetUserDetailsQuery { Id = userId }, cancellationToken);
        return Ok(response.Adapt<UserDetailsModel>());
    }

    /// <summary>The signed-in user's sessions.</summary>
    /// <param name="currentActor"></param>
    /// <param name="request">Filters and paging</param>
    /// <param name="cancellationToken"></param>
    /// <returns>User Sessions</returns>
    [HttpGet("Sessions", Name = nameof(GetMySessions))]
    [ProducesResponseType(typeof(IEnumerable<UserSessionModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMySessions(
        [FromServices] ICurrentActor currentActor,
        [FromQuery] GetUserSessionsListModel request,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        var query = request.Adapt<GetUserSessionsListQuery>();
        query.UserId = userId;
        var response = await Mediator.Send(query, cancellationToken);
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages,
            response.LastKeyProperty, response.LastKeyValue, response.HasMore);
        return Ok(response.Items.Adapt<IEnumerable<UserSessionModel>>());
    }

    /// <summary>
    /// The names of every permission the signed-in user holds, for the front end to cache and check locally
    /// (e.g. show or hide actions). The API still enforces each permission on every call.
    /// </summary>
    /// <param name="currentActor"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Permission names</returns>
    [HttpGet("Permissions", Name = nameof(GetMyPermissions))]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPermissions(
        [FromServices] ICurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        return Ok(await Mediator.Send(new GetMyPermissionsQuery { UserId = userId }, cancellationToken));
    }
}
