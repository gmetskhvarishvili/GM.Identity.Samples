using GM.EntityFramework.Domain.Common;
using GM.Identity.Sample.Application.Common.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using System;
using System.Threading.Tasks;

namespace GM.Identity.Sample.API.Authorization;

/// <summary>
/// Requires the calling user to hold the named permission (by convention the action's own name) through
/// one of their roles. The user id is taken from the ambient <see cref="ICurrentActor"/> (the
/// gateway-forwarded <c>X-User-Id</c> header or the token's NameIdentifier claim); the permission name is
/// resolved to its id and checked entirely from the Redis RBAC projection (<see cref="IPermissionCache"/>)
/// — no database round-trip. Returns 401 when there is no identity, 403 when the permission is unknown or
/// not granted. Repeatable to require several permissions (AND). The permission is seeded (name = action
/// name) by the startup seeder, which discovers it from this attribute.
/// </summary>
/// <example><c>[HasPermission(nameof(AddPermission))]</c></example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : TypeFilterAttribute
{
    public HasPermissionAttribute(string permissionName) : base(typeof(HasPermissionFilter))
    {
        PermissionName = permissionName;
        Arguments = [permissionName];
    }

    /// <summary>The required permission's name (read by the seeder to provision permissions).</summary>
    public string PermissionName { get; }
}

internal sealed class HasPermissionFilter(
    string permissionName,
    ICurrentActor currentActor,
    IPermissionCache permissionCache) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var userId = currentActor.UserId;
        if (userId is null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
            return;
        }

        var permissionId = await permissionCache.GetPermissionIdByNameAsync(
            permissionName, context.HttpContext.RequestAborted);

        // Unknown permission (not seeded) → deny rather than fail open.
        if (permissionId is null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }

        var granted = await permissionCache.HasPermissionAsync(
            userId.Value, permissionId.Value, context.HttpContext.RequestAborted);

        if (!granted)
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}
