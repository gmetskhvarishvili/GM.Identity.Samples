using GM.EntityFramework.Domain.Common;
using GM.Identity.Sample.Application.Common.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using System;
using System.Threading.Tasks;

namespace GM.Identity.Sample.API.Authorization;

/// <summary>
/// Requires the calling application (OAuth client) to be granted a scope that covers the named operation.
/// The client id is taken from the ambient <see cref="ICurrentActor"/> (the gateway-forwarded
/// <c>X-Client-Id</c> header, set from the validated token's client) and the operation name is resolved to
/// its id and checked entirely from the Redis scope projection (<see cref="IScopeCache"/>) — no database
/// round-trip. A token is authorized when <b>any</b> scope granted to its client covers the operation,
/// enforcing the Client → Scope → Operation chain. Returns 401 when there is no client identity, 403 when
/// the operation is unknown or not covered.
///
/// This is the client/application dimension of authorization and is orthogonal to
/// <see cref="HasPermissionAttribute"/> (the user dimension): an endpoint carrying both is reachable only
/// when the acting user holds the permission <em>and</em> the calling client is granted the scope.
/// </summary>
/// <example><c>[RequiresScope("read:identity")]</c></example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequiresScopeAttribute : TypeFilterAttribute
{
    public RequiresScopeAttribute(string operationName) : base(typeof(RequiresScopeFilter))
    {
        OperationName = operationName;
        Arguments = [operationName];
    }

    /// <summary>The operation the endpoint performs (must be covered by one of the client's scopes).</summary>
    public string OperationName { get; }
}

internal sealed class RequiresScopeFilter(
    string operationName,
    ICurrentActor currentActor,
    IScopeCache scopeCache) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var clientId = currentActor.ClientId;
        if (clientId is null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
            return;
        }

        var operationId = await scopeCache.GetOperationIdByNameAsync(
            operationName, context.HttpContext.RequestAborted);

        // Unknown operation (not seeded) → deny rather than fail open.
        if (operationId is null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }

        var granted = await scopeCache.HasOperationAsync(
            clientId.Value, operationId.Value, context.HttpContext.RequestAborted);

        if (!granted)
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}
