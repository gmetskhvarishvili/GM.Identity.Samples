using Microsoft.AspNetCore.Authorization;

namespace GM.Identity.Sample.Gateway.API.Authorization;

public class GrantTypeRequirement(string grantType) : IAuthorizationRequirement
{
    public string RequiredGrantType { get; } = grantType;
}