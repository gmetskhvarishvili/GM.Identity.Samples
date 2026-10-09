using System;
using System.Threading.Tasks;
using GM.EntityFramework.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GM.Identity.Sample.Service.API.Authorization;

/// <summary>
/// Requires the calling client to be granted a scope covering the named operation. The decision is delegated
/// to the Identity provider over HTTP (not read from a local cache): 401 when there is no client, 403 when the
/// provider says it is not covered. Repeatable to require several operations (AND).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireScopeAttribute : TypeFilterAttribute
{
    public RequireScopeAttribute(string operation) : base(typeof(RequireScopeFilter))
    {
        Operation = operation;
        Arguments = [operation];
    }

    public string Operation { get; }
}

internal sealed class RequireScopeFilter(
    string operation, ICurrentActor currentActor, IIdentityProviderClient authorization) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var clientId = currentActor.ClientId;
        if (clientId is null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
            return;
        }

        var granted = await authorization.HasScopeAsync(
            clientId.Value, operation, context.HttpContext.RequestAborted);

        if (!granted)
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}
