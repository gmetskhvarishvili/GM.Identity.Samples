using Asp.Versioning;
using GM.API.Controllers;
using GM.Identity.Sample.Application.Permissions.Commands.CreatePermission;
using GM.Identity.Sample.Application.Permissions.Commands.DeletePermission;
using GM.Identity.Sample.Application.Permissions.Commands.UpdatePermission;
using GM.Identity.Sample.Application.Permissions.Queries.GetPermissionDetails;
using GM.Identity.Sample.Application.Permissions.Queries.GetPermissionsList;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace GM.Identity.Sample.API.Permissions;

/// <summary>
/// Permissions Controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PermissionsController : BaseController
{
    /// <summary>
    /// Add Permission
    /// </summary>
    /// <param name="request">Permission Model to Add</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Permission Id</returns>
    [HttpPost(Name = nameof(AddPermission))]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddPermission(
        [FromBody] CreatePermissionModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreatePermissionCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update Permission
    /// </summary>
    /// <param name="id">Permission Id to Update</param>
    /// <param name="request">Permission Model to Update</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpPut("{id}", Name = nameof(UpdatePermission))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePermission(
        [FromRoute] Guid id,
        [FromBody] UpdatePermissionModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdatePermissionCommand>();
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Delete Permission
    /// </summary>
    /// <param name="id">Permission Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}", Name = nameof(DeletePermission))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePermission(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeletePermissionCommand
        {
            Id = id
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Get Permissions List
    /// </summary>
    /// <param name="request">Permission Model to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>IEnumerable of Permissions</returns>
    [HttpGet(Name = nameof(GetPermissionsList))]
    [ProducesResponseType(typeof(IEnumerable<PermissionModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionsList(
        [FromQuery] GetPermissionsListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetPermissionsListQuery>();
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<PermissionModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }

    /// <summary>
    /// Get Permission Details
    /// </summary>
    /// <param name="id">Permission Id to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Permission Details</returns>
    [HttpGet("{id}", Name = nameof(GetPermissionDetails))]
    [ProducesResponseType(typeof(PermissionDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermissionDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetPermissionDetailsQuery
        {
            Id = id
        };
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Adapt<PermissionDetailsModel>();
        return Ok(result);
    }
}