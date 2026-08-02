using Microsoft.AspNetCore.Authorization;

namespace GM.Identity.Sample.Gateway.API.Authorization;

public class GrantTypeAuthorizationHandler(ITokenManager tokenManager) : AuthorizationHandler<GrantTypeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GrantTypeRequirement requirement)
    {
        var grantType = tokenManager.GetClaim("grant_type");

        if (!string.IsNullOrEmpty(grantType) &&
            string.Equals(grantType, requirement.RequiredGrantType, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}