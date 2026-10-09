using System;
using System.Threading.Tasks;
using GM.EntityFramework.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GM.Identity.Sample.Service.API.Authorization;

/// <summary>
/// Requires the calling user to hold the named permission. The decision is delegated to the Identity provider
/// over HTTP (not read from a local cache): 401 when there is no user, 403 when the provider says it is not
/// granted. Repeatable to require several permissions (AND).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string name) : base(typeof(RequirePermissionFilter))
    {
        Name = name;
        Arguments = [name];
    }

    public string Name { get; }
}

internal sealed class RequirePermissionFilter(
    string name, ICurrentActor currentActor, IIdentityProviderClient authorization) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var userId = currentActor.UserId;
        if (userId is null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
            return;
        }

        var granted = await authorization.HasPermissionAsync(
            userId.Value, name, context.HttpContext.RequestAborted);

        if (!granted)
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}
