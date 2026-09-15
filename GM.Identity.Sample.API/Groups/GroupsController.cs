using Asp.Versioning;
using GM.API.Authorization;
using GM.API.Controllers;
using GM.Identity.Sample.Application.Groups.Commands.AddGroupRole;
using GM.Identity.Sample.Application.Groups.Commands.AddUserToGroup;
using GM.Identity.Sample.Application.Groups.Commands.CreateGroup;
using GM.Identity.Sample.Application.Groups.Commands.RemoveGroupRole;
using GM.Identity.Sample.Application.Groups.Commands.RemoveUserFromGroup;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.API.Groups;

/// <summary>
/// Groups Controller — manage groups (teams), the roles attached to them, and their members. Users inherit the
/// roles of every group they belong to.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class GroupsController : BaseController
{
    /// <summary>Create a group.</summary>
    [HasPermission(nameof(AddGroup))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost(Name = nameof(AddGroup))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddGroup([FromBody] CreateGroupModel request, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(new CreateGroupCommand { Name = request.Name }, cancellationToken);
        return Ok(id);
    }

    /// <summary>Grant a role to a group.</summary>
    [HasPermission(nameof(AddGroupRole))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Roles/{roleId}", Name = nameof(AddGroupRole))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddGroupRole([FromRoute] Guid id, [FromRoute] Guid roleId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new AddGroupRoleCommand { GroupId = id, RoleId = roleId }, cancellationToken);
        return Ok();
    }

    /// <summary>Remove a role from a group.</summary>
    [HasPermission(nameof(RemoveGroupRole))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpDelete("{id}/Roles/{roleId}", Name = nameof(RemoveGroupRole))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveGroupRole([FromRoute] Guid id, [FromRoute] Guid roleId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RemoveGroupRoleCommand { GroupId = id, RoleId = roleId }, cancellationToken);
        return Ok();
    }

    /// <summary>Add a user to a group.</summary>
    [HasPermission(nameof(AddUserToGroup))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Users/{userId}", Name = nameof(AddUserToGroup))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddUserToGroup([FromRoute] Guid id, [FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new AddUserToGroupCommand { GroupId = id, UserId = userId }, cancellationToken);
        return Ok();
    }

    /// <summary>Remove a user from a group.</summary>
    [HasPermission(nameof(RemoveUserFromGroup))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpDelete("{id}/Users/{userId}", Name = nameof(RemoveUserFromGroup))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveUserFromGroup([FromRoute] Guid id, [FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RemoveUserFromGroupCommand { GroupId = id, UserId = userId }, cancellationToken);
        return Ok();
    }
}
