using System;
using System.Threading;
using System.Threading.Tasks;
using GM.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GM.Identity.Sample.API.Authorization;

/// <summary>
/// Authorization-decision endpoints for resource servers. A resource server (e.g. the Service API) delegates
/// its permission and scope checks here over HTTP instead of reading the provider's projections directly —
/// this API is the source of truth for permissions and scopes. The lookups resolve the name to its id and
/// check the RBAC / scope projection.
/// </summary>
/// <remarks>
/// These endpoints disclose whether a given user/client is granted something, so in production restrict them
/// (internal network, mTLS, or a service credential). They are anonymous here to keep the sample simple.
/// </remarks>
[ApiController]
public sealed class AuthorizationController(
    IPermissionCache permissionCache, IScopeCache scopeCache) : ControllerBase
{
    /// <summary>Whether the user holds the named permission (user / RBAC dimension).</summary>
    [HttpGet("~/connect/authz/permission")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckPermission(
        [FromQuery] Guid userId, [FromQuery] string name, CancellationToken cancellationToken)
    {
        var permissionId = await permissionCache.GetPermissionIdByNameAsync(name, cancellationToken);
        var granted = permissionId is not null
                      && await permissionCache.HasPermissionAsync(userId, permissionId.Value, cancellationToken);

        return Ok(new AuthorizationDecision(granted));
    }

    /// <summary>Whether the client is granted a scope covering the named operation (client dimension).</summary>
    [HttpGet("~/connect/authz/scope")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckScope(
        [FromQuery] Guid clientId, [FromQuery] string operation, CancellationToken cancellationToken)
    {
        var operationId = await scopeCache.GetOperationIdByNameAsync(operation, cancellationToken);
        var granted = operationId is not null
                      && await scopeCache.HasOperationAsync(clientId, operationId.Value, cancellationToken);

        return Ok(new AuthorizationDecision(granted));
    }
}

/// <summary>The outcome of an authorization check.</summary>
public sealed record AuthorizationDecision(bool Granted);
