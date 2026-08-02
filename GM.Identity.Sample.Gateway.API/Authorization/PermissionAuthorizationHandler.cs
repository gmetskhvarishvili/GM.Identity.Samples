using Microsoft.AspNetCore.Authorization;

namespace GM.Identity.Sample.Gateway.API.Authorization;

public class PermissionAuthorizationHandler(ITokenManager tokenManager) : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var permissions = tokenManager.GetClaim("permissions");

        if (!string.IsNullOrEmpty(permissions) 
            && permissions.Contains(requirement.Permission, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}