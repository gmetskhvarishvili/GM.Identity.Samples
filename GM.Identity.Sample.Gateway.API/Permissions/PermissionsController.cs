using Asp.Versioning;
using GM.API.Controllers;
using GM.Identity.Sample.Gateway.API.Services.Permissions;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace GM.Identity.Sample.Gateway.API.Permissions;

/// <summary>
/// Permissions Controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PermissionsController(IPermissionsService permissionsService) : BaseController
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
        var requestModel = request.Adapt<CreatePermissionRequestModel>();
        var result = await permissionsService.CreatePermission(requestModel, cancellationToken);
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
        [FromRoute] string id,
        [FromBody] UpdatePermissionModel request,
        CancellationToken cancellationToken)
    {
        var requestModel = request.Adapt<UpdatePermissionRequestModel>();
        await permissionsService.UpdatePermission(id, requestModel, cancellationToken);
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
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        await permissionsService.DeletePermission(id, cancellationToken);
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
        var requestModel = request.Adapt<GetPermissionsListRequestModel>();
        var response = await permissionsService.GetPermissionsList(requestModel, cancellationToken);
        var result = response.Adapt<IEnumerable<PermissionModel>>();
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
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        var response = await permissionsService.GetPermissionDetails(id, cancellationToken);
        var result = response.Adapt<PermissionDetailsModel>();
        return Ok(result);
    }
}