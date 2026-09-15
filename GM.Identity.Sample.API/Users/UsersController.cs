using Asp.Versioning;
using GM.API.Controllers;
using GM.API.Models;
using GM.EntityFramework.Domain.Common;
using GM.Identity.Sample.API.Roles;
using GM.Identity.Sample.Application.Users.Commands.BulkSetUserBlock;
using GM.Identity.Sample.Application.Users.Commands.ChangeCurrentUserPassword;
using GM.Identity.Sample.Application.Users.Commands.ConfirmUser;
using GM.Identity.Sample.Application.Users.Commands.ConfirmUserInit;
using GM.Identity.Sample.Application.Users.Commands.CreateUser;
using GM.Identity.Sample.Application.Users.Commands.CreateUserRole;
using GM.Identity.Sample.Application.Users.Commands.DeleteAllUserSessions;
using GM.Identity.Sample.Application.Users.Commands.DeleteCurrentUser;
using GM.Identity.Sample.Application.Users.Commands.DeleteUser;
using GM.Identity.Sample.Application.Users.Commands.DeleteUserRole;
using GM.Identity.Sample.Application.Users.Commands.ConfirmContactChange;
using GM.Identity.Sample.Application.Users.Commands.ConfirmUserTotp;
using GM.Identity.Sample.Application.Users.Commands.CreateApiKey;
using GM.Identity.Sample.Application.Users.Commands.RevokeApiKey;
using GM.Identity.Sample.Application.Users.Commands.ConfirmUserTwoFactor;
using GM.Identity.Sample.Application.Users.Commands.DeleteUserSession;
using GM.Identity.Sample.Application.Users.Commands.DisableUserTotp;
using GM.Identity.Sample.Application.Users.Commands.DisableUserTwoFactor;
using GM.Identity.Sample.Application.Users.Commands.EnableUserTwoFactor;
using GM.Identity.Sample.Application.Users.Commands.GenerateRecoveryCodes;
using GM.Identity.Sample.Application.Users.Commands.GrantTimeBoundRole;
using GM.Identity.Sample.Application.Users.Commands.GrantUserPermission;
using GM.Identity.Sample.Application.Users.Commands.ImpersonateUser;
using GM.Identity.Sample.Application.Users.Commands.RevokeTimeBoundRole;
using GM.Identity.Sample.Application.Users.Commands.RevokeUserPermission;
using GM.Identity.Sample.Application.Users.Commands.SetupUserTotp;
using GM.Identity.Sample.Application.Users.Commands.LogoutAllUserSessions;
using GM.Identity.Sample.Application.Users.Commands.MergeUsers;
using GM.Identity.Sample.Application.Users.Commands.RecordUserConsent;
using GM.Identity.Sample.Application.Users.Commands.RequestContactChange;
using GM.Identity.Sample.Application.Users.Commands.LogoutCurrentUser;
using GM.Identity.Sample.Application.Users.Commands.SetUserActive;
using GM.Identity.Sample.Application.Users.Commands.SetUserBlock;
using GM.Identity.Sample.Application.Users.Commands.UnlockUser;
using GM.Identity.Sample.Application.Users.Commands.RecoverUserPassword;
using GM.Identity.Sample.Application.Users.Commands.ResetUserPassword;
using GM.Identity.Sample.Application.Users.Commands.UpdateUser;
using GM.Identity.Sample.Application.Users.Commands.UpdateUserPassword;
using GM.Identity.Sample.Application.Users.Queries.ExportCurrentUserData;
using GM.Identity.Sample.Application.Users.Queries.GetUserAuditTrail;
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
    // ---- Current user (self) — owner-scoped, no admin permission required. The user id comes from the
    // gateway-forwarded X-User-Id header (ICurrentActor), never from the route, so a caller can only ever
    // read/change their own data. These back the gateway's /me and /me/sessions routes. ----

    /// <summary>
    /// Get the current user's own details.
    /// </summary>
    [HttpGet("me", Name = nameof(GetCurrentUser))]
    [ProducesResponseType(typeof(UserDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(
        [FromServices] ICurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        var response = await Mediator.Send(new GetUserDetailsQuery { Id = userId }, cancellationToken);
        return Ok(response.Adapt<UserDetailsModel>());
    }

    /// <summary>
    /// Get the current user's own sessions.
    /// </summary>
    [HttpGet("me/Sessions", Name = nameof(GetCurrentUserSessions))]
    [ProducesResponseType(typeof(IEnumerable<UserSessionModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUserSessions(
        [FromServices] ICurrentActor currentActor,
        [FromQuery] GetUserSessionsListModel request,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        var query = request.Adapt<GetUserSessionsListQuery>();
        query.UserId = userId;
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<UserSessionModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }

    /// <summary>
    /// Change the current user's own password.
    /// </summary>
    [HttpPut("me/Password", Name = nameof(UpdateCurrentUserPassword))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateCurrentUserPassword(
        [FromServices] ICurrentActor currentActor,
        [FromBody] ChangeCurrentUserPasswordModel request,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        var command = request.Adapt<ChangeCurrentUserPasswordCommand>();
        command.UserId = userId;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Update the current user's own profile (username / email / phone).
    /// </summary>
    [HttpPut("me/Profile", Name = nameof(UpdateCurrentUser))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateCurrentUser(
        [FromServices] ICurrentActor currentActor,
        [FromBody] UpdateUserModel request,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        var command = request.Adapt<UpdateUserCommand>();
        command.Id = userId;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Log the current user out — revokes the session the presented token belongs to.
    /// </summary>
    [HttpPost("me/Logout", Name = nameof(LogoutCurrentUser))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutCurrentUser(
        [FromServices] ICurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        if (currentActor.SessionId is not { } sessionId)
            return Unauthorized();

        await Mediator.Send(new LogoutCurrentUserCommand { SessionId = sessionId }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Log the current user out everywhere — revokes all of their active sessions across every device.
    /// </summary>
    [HttpPost("me/Logout/All", Name = nameof(LogoutCurrentUserEverywhere))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutCurrentUserEverywhere(
        [FromServices] ICurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        await Mediator.Send(new LogoutAllUserSessionsCommand { UserId = userId }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Request a change to the current user's email or phone. Sends a one-time code to the NEW contact; the
    /// change is not applied until it's confirmed.
    /// </summary>
    [HttpPost("me/Contact/Change", Name = nameof(RequestContactChange))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RequestContactChange(
        [FromServices] ICurrentActor currentActor,
        [FromBody] RequestContactChangeModel request,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        await Mediator.Send(new RequestContactChangeCommand
        {
            UserId = userId,
            ConfirmationType = request.ConfirmationType,
            NewContact = request.NewContact,
        }, cancellationToken);
        return Ok();
    }

    /// <summary>Confirm a pending email/phone change with the code sent to the new contact (applies the change).</summary>
    [HttpPost("me/Contact/Confirm", Name = nameof(ConfirmContactChange))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmContactChange(
        [FromServices] ICurrentActor currentActor,
        [FromBody] ConfirmContactChangeModel request,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        await Mediator.Send(new ConfirmContactChangeCommand { UserId = userId, Code = request.Code }, cancellationToken);
        return Ok();
    }

    /// <summary>Record the current user's acceptance of a consent document (e.g. Terms of Service).</summary>
    [HttpPost("me/Consents", Name = nameof(RecordCurrentUserConsent))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RecordCurrentUserConsent(
        [FromServices] ICurrentActor currentActor,
        [FromBody] RecordConsentModel request,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        await Mediator.Send(new RecordUserConsentCommand
        {
            UserId = userId,
            ConsentType = request.ConsentType,
            DocumentVersion = request.DocumentVersion,
        }, cancellationToken);
        return Ok();
    }

    /// <summary>List the current user's recorded consent acceptances.</summary>
    [HttpGet("me/Consents", Name = nameof(GetCurrentUserConsents))]
    [ProducesResponseType(typeof(IReadOnlyList<UserConsentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUserConsents(
        [FromServices] ICurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        var result = await Mediator.Send(new GetUserConsentsQuery { UserId = userId }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Export everything this identity server holds about the current user (GDPR-style data portability):
    /// profile, roles, 2FA enrolments, and active sessions. Secrets and password hashes are excluded.
    /// </summary>
    [HttpGet("me/Export", Name = nameof(ExportCurrentUserData))]
    [ProducesResponseType(typeof(UserDataExportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportCurrentUserData(
        [FromServices] ICurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        var result = await Mediator.Send(new ExportCurrentUserDataQuery { UserId = userId }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Close the current user's own account (self-service). Soft-deletes the account and revokes all sessions.
    /// </summary>
    [HttpDelete("me", Name = nameof(DeleteCurrentUser))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCurrentUser(
        [FromServices] ICurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } userId)
            return Unauthorized();

        await Mediator.Send(new DeleteCurrentUserCommand { UserId = userId }, cancellationToken);
        return Ok();
    }

    // ---- Account state (admin) — block/unblock, activate/deactivate, unlock. Each finds the user
    // regardless of active/blocked state; the session-revoking ones take effect immediately. ----

    /// <summary>
    /// Merge a source user into a target: moves the source's roles, groups, and direct permissions to the
    /// target, revokes the source's sessions, and soft-deletes the source.
    /// </summary>
    [HasPermission(nameof(MergeUsers))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Merge/{sourceId}", Name = nameof(MergeUsers))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MergeUsers(
        [FromRoute] Guid id, [FromRoute] Guid sourceId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new MergeUsersCommand { TargetUserId = id, SourceUserId = sourceId }, cancellationToken);
        return Ok();
    }

    /// <summary>Bulk block/unblock many users in one operation. Returns the number of users changed.</summary>
    [HasPermission(nameof(BulkSetUserBlock))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("Bulk/Block", Name = nameof(BulkSetUserBlock))]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkSetUserBlock(
        [FromBody] BulkSetUserBlockModel request, CancellationToken cancellationToken)
    {
        var changed = await Mediator.Send(
            new BulkSetUserBlockCommand { UserIds = request.UserIds, Block = request.Block }, cancellationToken);
        return Ok(changed);
    }

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

    /// <summary>Confirm a pending second-factor enrolment with the one-time setup code (this activates it).</summary>
    [HasPermission(nameof(ConfirmUserTwoFactor))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/TwoFactor/{twoFactorAuthTypeId:int}/Confirm", Name = nameof(ConfirmUserTwoFactor))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmUserTwoFactor(
        [FromRoute] Guid id,
        [FromRoute] int twoFactorAuthTypeId,
        [FromBody] ConfirmUserTwoFactorModel request,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new ConfirmUserTwoFactorCommand
            {
                UserId = id,
                TwoFactorAuthTypeId = twoFactorAuthTypeId,
                Code = request.Code,
            },
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

    /// <summary>Issue a personal access token (API key) for a user. Returns the plaintext key exactly once.</summary>
    [HasPermission(nameof(CreateApiKey))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/ApiKeys", Name = nameof(CreateApiKey))]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateApiKey(
        [FromRoute] Guid id, [FromBody] CreateApiKeyModel request, CancellationToken cancellationToken)
    {
        var key = await Mediator.Send(
            new CreateApiKeyCommand { UserId = id, Name = request.Name, ExpiresAt = request.ExpiresAt },
            cancellationToken);
        return Ok(key);
    }

    /// <summary>Revoke one of a user's API keys.</summary>
    [HasPermission(nameof(RevokeApiKey))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpDelete("{id}/ApiKeys/{apiKeyId}", Name = nameof(RevokeApiKey))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeApiKey(
        [FromRoute] Guid id, [FromRoute] Guid apiKeyId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RevokeApiKeyCommand { UserId = id, ApiKeyId = apiKeyId }, cancellationToken);
        return Ok();
    }

    /// <summary>Grant a role to a user until an expiry time (temporary/elevated access).</summary>
    [HasPermission(nameof(GrantTimeBoundRole))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/TimeBoundRoles/{roleId}", Name = nameof(GrantTimeBoundRole))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GrantTimeBoundRole(
        [FromRoute] Guid id, [FromRoute] Guid roleId, [FromBody] GrantTimeBoundRoleModel request, CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new GrantTimeBoundRoleCommand { UserId = id, RoleId = roleId, ExpiresAt = request.ExpiresAt },
            cancellationToken);
        return Ok();
    }

    /// <summary>Revoke a time-bound role grant before it expires.</summary>
    [HasPermission(nameof(RevokeTimeBoundRole))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpDelete("{id}/TimeBoundRoles/{roleId}", Name = nameof(RevokeTimeBoundRole))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeTimeBoundRole(
        [FromRoute] Guid id, [FromRoute] Guid roleId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RevokeTimeBoundRoleCommand { UserId = id, RoleId = roleId }, cancellationToken);
        return Ok();
    }

    /// <summary>Grant a permission directly to a user (in addition to role-derived permissions).</summary>
    [HasPermission(nameof(GrantUserPermission))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Permissions/{permissionId}", Name = nameof(GrantUserPermission))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GrantUserPermission(
        [FromRoute] Guid id, [FromRoute] Guid permissionId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new GrantUserPermissionCommand { UserId = id, PermissionId = permissionId }, cancellationToken);
        return Ok();
    }

    /// <summary>Revoke a directly-granted user permission.</summary>
    [HasPermission(nameof(RevokeUserPermission))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpDelete("{id}/Permissions/{permissionId}", Name = nameof(RevokeUserPermission))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeUserPermission(
        [FromRoute] Guid id, [FromRoute] Guid permissionId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RevokeUserPermissionCommand { UserId = id, PermissionId = permissionId }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Impersonate a user (support "log in as"): issues a session that authenticates as the target user, under
    /// the admin's client, tagged with the acting admin for audit. Requires an authenticated admin session.
    /// </summary>
    [HasPermission(nameof(ImpersonateUser))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Impersonate", Name = nameof(ImpersonateUser))]
    [ProducesResponseType(typeof(ImpersonationTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImpersonateUser(
        [FromRoute] Guid id,
        [FromServices] ICurrentActor currentActor,
        CancellationToken cancellationToken)
    {
        if (currentActor.UserId is not { } adminId || currentActor.ClientId is not { } clientId)
            return Unauthorized();

        var result = await Mediator.Send(new ImpersonateUserCommand
        {
            TargetUserId = id,
            ClientId = clientId,
            ImpersonatorUserId = adminId,
        }, cancellationToken);
        return Ok(result);
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
    /// (Re)generate a user's single-use backup codes. Returns the new codes exactly once — they replace any
    /// existing set and are never recoverable afterwards.
    /// </summary>
    [HasPermission(nameof(GenerateRecoveryCodes))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/RecoveryCodes", Name = nameof(GenerateRecoveryCodes))]
    [ProducesResponseType(typeof(RecoveryCodesModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateRecoveryCodes([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var codes = await Mediator.Send(new GenerateRecoveryCodesCommand { UserId = id }, cancellationToken);
        return Ok(new RecoveryCodesModel { Codes = codes });
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
    /// Get a user's audit trail — the domain-event history recorded for that user, newest first and paged.
    /// </summary>
    /// <param name="id">User Id whose audit trail to read</param>
    /// <param name="request">Paging options</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The user's recorded domain events</returns>
    [HasPermission(nameof(GetUserAuditTrail))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
    [HttpGet("{id}/AuditTrail", Name = nameof(GetUserAuditTrail))]
    [ProducesResponseType(typeof(IEnumerable<UserAuditTrailModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserAuditTrail(
        [FromRoute] Guid id,
        [FromQuery] GetUserAuditTrailModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetUserAuditTrailQuery>();
        query.UserId = id;
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<UserAuditTrailModel>>();
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
