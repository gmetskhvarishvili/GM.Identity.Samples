using Asp.Versioning;
using GM.API.Controllers;
using GM.API.Models;
using GM.Identity.Sample.API.Permissions;
using GM.Identity.Sample.Application.Roles.Commands.CreateRole;
using GM.Identity.Sample.Application.Roles.Commands.CreateRolePermission;
using GM.Identity.Sample.Application.Roles.Commands.DeleteRole;
using GM.Identity.Sample.Application.Roles.Commands.DeleteRolePermission;
using GM.Identity.Sample.Application.Roles.Commands.UpdateRole;
using GM.Identity.Sample.Application.Roles.Queries.GetRoleDetails;
using GM.Identity.Sample.Application.Roles.Queries.GetRolePermissionsList;
using GM.Identity.Sample.Application.Roles.Queries.GetRolesList;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace GM.Identity.Sample.API.Roles;

/// <summary>
/// Roles Controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RolesController : BaseController
{
    /// <summary>
    /// Add Role
    /// </summary>
    /// <param name="request">Role Model to Add</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Role Id</returns>
    [HttpPost(Name = nameof(AddRole))]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddRole(
        [FromBody] CreateRoleModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateRoleCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }
    
    /// <summary>
    /// Add Role Permission
    /// </summary>
    /// <param name="id">Role Id to Update</param>
    /// <param name="request">Role Permission Model to Add</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Role Id</returns>
    [HttpPost("{id}/Permissions", Name = nameof(AddRolePermission))]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddRolePermission(
        [FromRoute] Guid id,
        [FromBody] CreateRolePermissionModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateRolePermissionCommand>();
        command.RoleId = id;
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update Role
    /// </summary>
    /// <param name="id">Role Id to Update</param>
    /// <param name="request">Role Model to Update</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpPut("{id}", Name = nameof(UpdateRole))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(
        [FromRoute] Guid id,
        [FromBody] UpdateRoleModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateRoleCommand>();
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Delete Role
    /// </summary>
    /// <param name="id">Role Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}", Name = nameof(DeleteRole))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRoleCommand
        {
            Id = id
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Delete Role Permission
    /// </summary>
    /// <param name="id">Role Id to Delete</param>
    /// <param name="permissionId">Permission Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}/Permissions/{permissionId}", Name = nameof(DeleteRolePermission))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRolePermission(
        [FromRoute] Guid id,
        [FromRoute] Guid permissionId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRolePermissionCommand
        {
            RoleId = id,
            PermissionId = permissionId
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Get Roles List
    /// </summary>
    /// <param name="request">Role Model to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>IEnumerable of Roles</returns>
    [HttpGet(Name = nameof(GetRolesList))]
    [ProducesResponseType(typeof(IEnumerable<RoleModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolesList(
        [FromQuery] GetRolesListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetRolesListQuery>();
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<RoleModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }

    /// <summary>
    /// Get Role Permissions List
    /// </summary>
    /// <param name="id">Role Id to Get</param>
    /// <param name="request">Role Permission Model to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Role Permissions</returns>
    [HttpGet("{id}/Permissions", Name = nameof(GetRolePermissions))]
    [ProducesResponseType(typeof(IEnumerable<PermissionModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRolePermissions(
        [FromRoute] Guid id,
        [FromQuery] GetBaseListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetRolePermissionsListQuery>();
        query.RoleId = id;
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<PermissionModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }

    /// <summary>
    /// Get Role Details
    /// </summary>
    /// <param name="id">Role Id to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Role Details</returns>
    [HttpGet("{id}", Name = nameof(GetRoleDetails))]
    [ProducesResponseType(typeof(RoleDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetRoleDetailsQuery
        {
            Id = id
        };
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Adapt<RoleDetailsModel>();
        return Ok(result);
    }
}