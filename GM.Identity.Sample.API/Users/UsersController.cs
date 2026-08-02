using Asp.Versioning;
using GM.API.Controllers;
using GM.API.Models;
using GM.Identity.Sample.API.Roles;
using GM.Identity.Sample.Application.Users.Commands.ConfirmUser;
using GM.Identity.Sample.Application.Users.Commands.ConfirmUserInit;
using GM.Identity.Sample.Application.Users.Commands.CreateUser;
using GM.Identity.Sample.Application.Users.Commands.CreateUserRole;
using GM.Identity.Sample.Application.Users.Commands.DeleteAllUserSessions;
using GM.Identity.Sample.Application.Users.Commands.DeleteUser;
using GM.Identity.Sample.Application.Users.Commands.DeleteUserRole;
using GM.Identity.Sample.Application.Users.Commands.DeleteUserSession;
using GM.Identity.Sample.Application.Users.Commands.RecoverUserPassword;
using GM.Identity.Sample.Application.Users.Commands.ResetUserPassword;
using GM.Identity.Sample.Application.Users.Commands.UpdateUser;
using GM.Identity.Sample.Application.Users.Commands.UpdateUserPassword;
using GM.Identity.Sample.Application.Users.Queries.GetUserDetails;
using GM.Identity.Sample.Application.Users.Queries.GetUserRolesList;
using GM.Identity.Sample.Application.Users.Queries.GetUserSessionsList;
using GM.Identity.Sample.Application.Users.Queries.GetUsersList;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace GM.Identity.Sample.API.Users;

/// <summary>
/// Users Controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : BaseController
{
    /// <summary>
    /// Add User
    /// </summary>
    /// <param name="request">User Model to Add</param>
    /// <param name="cancellationToken"></param>
    /// <returns>User Id</returns>
    [HttpPost(Name = nameof(AddUser))]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddUser(
        [FromBody] CreateUserModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateUserCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }
    
    /// <summary>
    /// Add User Role
    /// </summary>
    /// <param name="id">User Id to Update</param>
    /// <param name="request">User Role Model to Add</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpPost("{id}/Roles", Name = nameof(AddUserRole))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddUserRole(
        [FromRoute] Guid id,
        [FromBody] CreateUserRoleModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateUserRoleCommand>();
        command.UserId = id;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }
    
    /// <summary>
    /// Reset User Password
    /// </summary>
    /// <param name="request">Reset User Password</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpPost("Password/Reset", Name = nameof(ResetUserPassword))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetUserPassword(
        [FromBody] ResetUserPasswordModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<ResetUserPasswordCommand>();
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }
    
    /// <summary>
    /// Recover User Password
    /// </summary>
    /// <param name="request">Recover User Password</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpPost("Password/Recover", Name = nameof(RecoverUserPassword))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecoverUserPassword(
        [FromBody] RecoverUserPasswordModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<RecoverUserPasswordCommand>();
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Confirm User Init
    /// </summary>
    /// <param name="id">User Id to Confirm</param>
    /// <param name="request">Confirm User Init</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpPost("{id}/Confirm/Init", Name = nameof(ConfirmUserInit))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmUserInit(
        [FromRoute] Guid id,
        [FromBody] ConfirmUserInitModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<ConfirmUserInitCommand>();
        command.UserId = id;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Confirm User
    /// </summary>
    /// <param name="id">User Id to Confirm</param>
    /// <param name="request">Confirm User</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpPost("{id}/Confirm", Name = nameof(ConfirmUser))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmUser(
        [FromRoute] Guid id,
        [FromBody] ConfirmUserModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<ConfirmUserCommand>();
        command.UserId = id;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Update User
    /// </summary>
    /// <param name="id">User Id to Update</param>
    /// <param name="request">User Model to Update</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpPut("{id}", Name = nameof(UpdateUser))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(
        [FromRoute] Guid id,
        [FromBody] UpdateUserModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateUserCommand>();
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }
    
    /// <summary>
    /// Update User Password
    /// </summary>
    /// <param name="id">User Id to Update</param>
    /// <param name="request">User Model to Update</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpPut("{id}/Password", Name = nameof(UpdateUserPassword))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserPassword(
        [FromRoute] Guid id,
        [FromBody] UpdateUserPasswordModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateUserPasswordCommand>();
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Delete User
    /// </summary>
    /// <param name="id">User Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}", Name = nameof(DeleteUser))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteUserCommand
        {
            Id = id
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Delete User Role
    /// </summary>
    /// <param name="id">User Id to Delete</param>
    /// <param name="roleId">Role Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}/Roles/{roleId}", Name = nameof(DeleteUserRole))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserRole(
        [FromRoute] Guid id,
        [FromRoute] Guid roleId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteUserRoleCommand
        {
            UserId = id,
            RoleId = roleId,
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Delete User Sessions
    /// </summary>
    /// <param name="id">User Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}/Sessions", Name = nameof(DeleteUserSessions))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserSessions(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteAllUserSessionsCommand
        {
            UserId = id,
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }
    
    /// <summary>
    /// Delete User Session
    /// </summary>
    /// <param name="id">User Id to Delete</param>
    /// <param name="sessionId">User Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}/Sessions/{sessionId}", Name = nameof(DeleteUserSession))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserSession(
        [FromRoute] Guid id,
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteUserSessionCommand
        {
            UserId = id,
            Id = sessionId,
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    
    /// <summary>
    /// Get Users List
    /// </summary>
    /// <param name="request">User Model to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>IEnumerable of Roles</returns>
    [HttpGet(Name = nameof(GetUsersList))]
    [ProducesResponseType(typeof(IEnumerable<UserModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersList(
        [FromQuery] GetUsersListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetUsersListQuery>();
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<UserModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }

    /// <summary>
    /// Get User Roles List
    /// </summary>
    /// <param name="id">User Id to Get</param>
    /// <param name="request">User Role Model to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>User Roles</returns>
    [HttpGet("{id}/Roles", Name = nameof(GetUserRoles))]
    [ProducesResponseType(typeof(IEnumerable<RoleModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserRoles(
        [FromRoute] Guid id,
        [FromQuery] GetBaseListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetUserRolesListQuery>();
        query.UserId = id;
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<RoleModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }
    
    /// <summary>
    /// Get User Sessions List
    /// </summary>
    /// <param name="id">User Id to Get</param>
    /// <param name="request">User Session Model to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>User Sessions</returns>
    [HttpGet("{id}/Sessions", Name = nameof(GetUserSessions))]
    [ProducesResponseType(typeof(IEnumerable<UserSessionModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserSessions(
        [FromRoute] Guid id,
        [FromQuery] GetUserSessionsListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetUserSessionsListQuery>();
        query.UserId = id;
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<UserSessionModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }

    /// <summary>
    /// Get User Details
    /// </summary>
    /// <param name="id">User Id to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>User Details</returns>
    [HttpGet("{id}", Name = nameof(GetUserDetails))]
    [ProducesResponseType(typeof(UserDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetUserDetailsQuery
        {
            Id = id
        };
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Adapt<UserDetailsModel>();
        return Ok(result);
    }
}