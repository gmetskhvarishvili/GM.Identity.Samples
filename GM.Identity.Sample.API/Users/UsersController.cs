using Asp.Versioning;
using GM.API.Controllers;
using GM.API.Models;
using GM.EntityFramework.Domain.Common;
using GM.Identity.Sample.API.Roles;
using GM.Identity.Sample.Application.Users.Commands.ConfirmUser;
using GM.Identity.Sample.Application.Users.Commands.ConfirmUserInit;
using GM.Identity.Sample.Application.Users.Commands.CreateUser;
using GM.Identity.Sample.Application.Users.Commands.CreateUserRole;
using GM.Identity.Sample.Application.Users.Commands.DeleteAllUserSessions;
using GM.Identity.Sample.Application.Users.Commands.DeleteUser;
using GM.Identity.Sample.Application.Users.Commands.DeleteUserRole;
using GM.Identity.Sample.Application.Users.Commands.ConfirmUserTotp;
using GM.Identity.Sample.Application.Users.Commands.DeleteUserSession;
using GM.Identity.Sample.Application.Users.Commands.DisableUserTotp;
using GM.Identity.Sample.Application.Users.Commands.DisableUserTwoFactor;
using GM.Identity.Sample.Application.Users.Commands.EnableUserTwoFactor;
using GM.Identity.Sample.Application.Users.Commands.SetupUserTotp;
using GM.Identity.Sample.Application.Users.Commands.RecordUserConsent;
using GM.Identity.Sample.Application.Users.Commands.RegisterPasskey;
using GM.Identity.Sample.Application.Users.Commands.SetUserActive;
using GM.Identity.Sample.Application.Users.Commands.SetUserBlock;
using GM.Identity.Sample.Application.Users.Commands.UnlockUser;
using GM.Identity.Sample.Application.Users.Commands.RecoverUserPassword;
using GM.Identity.Sample.Application.Users.Commands.ResetUserPassword;
using GM.Identity.Sample.Application.Users.Commands.UpdateUser;
using GM.Identity.Sample.Application.Users.Commands.UpdateUserPassword;
using GM.Identity.Sample.Application.Users.Queries.GetPendingConsents;
using GM.Identity.Sample.Application.Users.Queries.GetUserConsents;
using GM.Identity.Sample.Application.Users.Queries.GetUserDetails;
using GM.Identity.Sample.Application.Users.Queries.GetUserRolesList;
using GM.Identity.Sample.Application.Users.Queries.GetUserSessionsList;
using GM.Identity.Sample.Application.Users.Queries.GetUsersList;
using Mapster;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GM.API.Authorization;
using GM.Identity.Sample.Domain.SeedWork;
namespace GM.Identity.Sample.API.Users;

/// <summary>
/// Users Controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : BaseController
{
    // ---- Per-user operations addressed by user id. Identity is a backend service: the caller passes the user
    // id explicitly (never derived from the session), and these are admin-gated like the rest of the API.
    // Reading/updating a user's own profile, sessions, account deletion and logout are the {id} endpoints below
    // (GetUserDetails, GetUserSessions, UpdateUser, DeleteUser, DeleteUserSessions/DeleteUserSession). ----

    /// <summary>Record a user's acceptance of a consent document (e.g. Terms of Service).</summary>
    [HasPermission(nameof(RecordUserConsent))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Consents", Name = nameof(RecordUserConsent))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordUserConsent(
        [FromRoute] Guid id,
        [FromBody] RecordConsentModel request,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new RecordUserConsentCommand
        {
            UserId = id,
            ConsentType = request.ConsentType,
            DocumentVersion = request.DocumentVersion,
        }, cancellationToken);
        return Ok();
    }

    /// <summary>List a user's recorded consent acceptances.</summary>
    [HasPermission(nameof(GetUserConsents))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
    [HttpGet("{id}/Consents", Name = nameof(GetUserConsents))]
    [ProducesResponseType(typeof(IReadOnlyList<UserConsentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserConsents(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetUserConsentsQuery { UserId = id }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// List the mandatory consent documents a user must still accept (never accepted, or an older version).
    /// </summary>
    [HasPermission(nameof(GetUserPendingConsents))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
    [HttpGet("{id}/Consents/Pending", Name = nameof(GetUserPendingConsents))]
    [ProducesResponseType(typeof(IReadOnlyList<PendingConsentModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserPendingConsents(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new GetPendingConsentsQuery { UserId = id }, cancellationToken);
        var result = response.Adapt<IReadOnlyList<PendingConsentModel>>();
        return Ok(result);
    }

    // ---- Account state (admin) — block/unblock, activate/deactivate, unlock. Each finds the user
    // regardless of active/blocked state; the session-revoking ones take effect immediately. ----

    /// <summary>Block a user (and revoke their active sessions).</summary>
    [HasPermission(nameof(BlockUser))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPut("{id}/Block", Name = nameof(BlockUser))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BlockUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new SetUserBlockCommand { UserId = id, Block = true }, cancellationToken);
        return Ok();
    }

    /// <summary>Unblock a user.</summary>
    [HasPermission(nameof(UnblockUser))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPut("{id}/Unblock", Name = nameof(UnblockUser))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnblockUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new SetUserBlockCommand { UserId = id, Block = false }, cancellationToken);
        return Ok();
    }

    /// <summary>Deactivate a user (and revoke their active sessions).</summary>
    [HasPermission(nameof(DeactivateUser))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPut("{id}/Deactivate", Name = nameof(DeactivateUser))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new SetUserActiveCommand { UserId = id, Active = false }, cancellationToken);
        return Ok();
    }

    /// <summary>Reactivate a user.</summary>
    [HasPermission(nameof(ReactivateUser))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPut("{id}/Activate", Name = nameof(ReactivateUser))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new SetUserActiveCommand { UserId = id, Active = true }, cancellationToken);
        return Ok();
    }

    /// <summary>Clear a user's failed-login lockout.</summary>
    [HasPermission(nameof(UnlockUser))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPut("{id}/Unlock", Name = nameof(UnlockUser))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new UnlockUserCommand { UserId = id }, cancellationToken);
        return Ok();
    }

    /// <summary>Enrol a user in a second-factor method (subsequent logins then require a 2FA challenge).</summary>
    [HasPermission(nameof(EnableUserTwoFactor))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/TwoFactor/{twoFactorAuthTypeId:int}", Name = nameof(EnableUserTwoFactor))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnableUserTwoFactor(
        [FromRoute] Guid id, [FromRoute] int twoFactorAuthTypeId, CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new EnableUserTwoFactorCommand { UserId = id, TwoFactorAuthTypeId = twoFactorAuthTypeId },
            cancellationToken);
        return Ok();
    }

    /// <summary>Remove a second-factor enrolment from a user.</summary>
    [HasPermission(nameof(DisableUserTwoFactor))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpDelete("{id}/TwoFactor/{twoFactorAuthTypeId:int}", Name = nameof(DisableUserTwoFactor))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DisableUserTwoFactor(
        [FromRoute] Guid id, [FromRoute] int twoFactorAuthTypeId, CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new DisableUserTwoFactorCommand { UserId = id, TwoFactorAuthTypeId = twoFactorAuthTypeId },
            cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Begin authenticator-app (TOTP) enrolment. Returns the shared secret and the otpauth:// URI to render as
    /// a QR code. The device is pending until confirmed with a code.
    /// </summary>
    [HasPermission(nameof(SetupUserTotp))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Totp/Setup", Name = nameof(SetupUserTotp))]
    [ProducesResponseType(typeof(SetupUserTotpResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetupUserTotp([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetupUserTotpCommand { UserId = id }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Confirm a pending authenticator-app enrolment with a code from the app (activates it).</summary>
    [HasPermission(nameof(ConfirmUserTotp))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Totp/Confirm", Name = nameof(ConfirmUserTotp))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmUserTotp(
        [FromRoute] Guid id, [FromBody] ConfirmUserTotpModel request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new ConfirmUserTotpCommand { UserId = id, Code = request.Code }, cancellationToken);
        return Ok();
    }

    /// <summary>Register a WebAuthn passkey for a user (stores the credential's public key).</summary>
    [HasPermission(nameof(RegisterPasskey))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Passkeys", Name = nameof(RegisterPasskey))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterPasskey(
        [FromRoute] Guid id, [FromBody] RegisterPasskeyModel request, CancellationToken cancellationToken)
    {
        var passkeyId = await Mediator.Send(new RegisterPasskeyCommand
        {
            UserId = id,
            CredentialId = request.CredentialId,
            PublicKeySpkiBase64 = request.PublicKeySpkiBase64,
            Name = request.Name,
        }, cancellationToken);
        return Ok(passkeyId);
    }

    /// <summary>Remove a user's authenticator-app (TOTP) device.</summary>
    [HasPermission(nameof(DisableUserTotp))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpDelete("{id}/Totp", Name = nameof(DisableUserTotp))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DisableUserTotp([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DisableUserTotpCommand { UserId = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Add User
    /// </summary>
    /// <param name="request">User Model to Add</param>
    /// <param name="cancellationToken"></param>
    /// <returns>User Id</returns>
    [HttpPost(Name = nameof(AddUser))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
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
    [HasPermission(nameof(AddUserRole))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
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
    [HasPermission(nameof(UpdateUser))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
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
    [HasPermission(nameof(UpdateUserPassword))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
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
    [HasPermission(nameof(DeleteUser))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
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
    [HasPermission(nameof(DeleteUserRole))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
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
    [HasPermission(nameof(DeleteUserSessions))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
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
    [HasPermission(nameof(DeleteUserSession))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
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
    [HasPermission(nameof(GetUsersList))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
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
    [HasPermission(nameof(GetUserRoles))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
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
    [HasPermission(nameof(GetUserSessions))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
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
    [HasPermission(nameof(GetUserDetails))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
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
