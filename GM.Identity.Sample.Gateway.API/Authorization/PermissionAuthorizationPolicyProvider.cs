using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace GM.Identity.Sample.Gateway.API.Authorization;

public class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase))
            return await _fallbackPolicyProvider.GetPolicyAsync(policyName);
        
        var permissionName = policyName["Permission:".Length..];

        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(permissionName))
            .Build();

        return policy;

    }
}
