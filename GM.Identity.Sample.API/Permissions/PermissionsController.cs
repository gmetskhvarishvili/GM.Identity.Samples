using Asp.Versioning;
using GM.API.Controllers;
using GM.Identity.Sample.Application.Permissions.Commands.RestorePermission;
using GM.Identity.Sample.Application.Permissions.Commands.ActivatePermission;
using GM.Identity.Sample.Application.Permissions.Commands.DeactivatePermission;
using GM.Identity.Sample.Application.Permissions.Commands.HidePermission;
using GM.Identity.Sample.Application.Permissions.Commands.UnhidePermission;
using GM.API.Models;
using GM.Identity.Sample.Application.Permissions.Commands.CreatePermission;
using GM.Identity.Sample.Application.Permissions.Commands.DeletePermission;
using GM.Identity.Sample.Application.Permissions.Commands.UpdatePermission;
using GM.Identity.Sample.Application.Permissions.Queries.GetPermissionDetails;
using GM.Identity.Sample.Application.Permissions.Queries.GetPermissionsList;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GM.API.Authorization;
using GM.Identity.Sample.Domain.SeedWork;

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
    [HasPermission(nameof(AddPermission))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost(Name = nameof(AddPermission))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
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
    [HasPermission(nameof(UpdatePermission))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
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
    [HasPermission(nameof(DeletePermission))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
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
    [HasPermission(nameof(GetPermissionsList))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
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
    /// <param name="request">Visibility scope (query string); defaults to visible-only.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Permission Details</returns>
    [HasPermission(nameof(GetPermissionDetails))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
    [HttpGet("{id}", Name = nameof(GetPermissionDetails))]
    [ProducesResponseType(typeof(PermissionDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPermissionDetails(
        [FromRoute] Guid id,
        [FromQuery] GetBaseDetailsModel request,
        CancellationToken cancellationToken)
    {
        var query = new GetPermissionDetailsQuery
        {
            Id = id,
            Visibility = request.Visibility
        };
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Adapt<PermissionDetailsModel>();
        return Ok(result);
    }

    /// <summary>
    /// Restore Permission
    /// </summary>
    /// <param name="id">Permission Id to restore</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(RestorePermission))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Restore", Name = nameof(RestorePermission))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestorePermission(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new RestorePermissionCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Activate Permission
    /// </summary>
    /// <param name="id">Permission Id to activate</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(ActivatePermission))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Activate", Name = nameof(ActivatePermission))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivatePermission(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new ActivatePermissionCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Deactivate Permission
    /// </summary>
    /// <param name="id">Permission Id to deactivate</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(DeactivatePermission))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Deactivate", Name = nameof(DeactivatePermission))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivatePermission(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeactivatePermissionCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Hide Permission
    /// </summary>
    /// <param name="id">Permission Id to hide</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(HidePermission))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Hide", Name = nameof(HidePermission))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HidePermission(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new HidePermissionCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Unhide Permission
    /// </summary>
    /// <param name="id">Permission Id to unhide</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(UnhidePermission))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Unhide", Name = nameof(UnhidePermission))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnhidePermission(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new UnhidePermissionCommand { Id = id }, cancellationToken);
        return Ok();
    }
}
