using GM.EntityFramework.Domain.Common;
using GM.Identity.Sample.Service.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GM.Identity.Sample.Service.API.Controllers;

/// <summary>
/// Dummy endpoints to exercise the three authorization dimensions against the GM Identity provider:
/// a valid token, the user's permissions, and the client's scopes. Pass an access token as
/// <c>Authorization: Bearer &lt;token&gt;</c> (get one from the provider's <c>/connect/token</c>).
/// </summary>
[ApiController]
[Route("api/demo")]
public sealed class DemoController(ICurrentActor actor) : ControllerBase
{
    /// <summary>Open to everyone — no token required. Confirms the service is up.</summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult Public() =>
        Ok(new { message = "Public endpoint — no authentication required." });

    /// <summary>Requires any valid access token (401 otherwise). Tests token validation.</summary>
    [HttpGet("token")]
    [Authorize]
    public IActionResult TokenOnly() =>
        Ok(new { message = "Token is valid.", actor = Actor() });

    /// <summary>Echoes the authenticated actor resolved from the token (or gateway headers).</summary>
    [HttpGet("whoami")]
    [Authorize]
    public IActionResult WhoAmI() => Ok(Actor());

    /// <summary>
    /// Requires the user to hold the <c>demo.read</c> permission (user dimension, RBAC). To make this pass:
    /// create a permission named <c>demo.read</c> on the provider, grant it to a role, and assign that role
    /// to your user. Returns 403 until then.
    /// </summary>
    [HttpGet("permission")]
    [Authorize]
    [RequirePermission(DemoAuthz.ReadDemoPermission)]
    public IActionResult RequiresPermission() =>
        Ok(new { message = $"Granted — you hold '{DemoAuthz.ReadDemoPermission}'.", actor = Actor() });

    /// <summary>
    /// Requires the calling client to be granted a scope covering <c>manage:identity</c> (client dimension).
    /// The sample's seeded client (identity.manage) passes out of the box; a client without it gets 403.
    /// </summary>
    [HttpGet("scope")]
    [Authorize]
    [RequireScope(DemoAuthz.ManageIdentityScope)]
    public IActionResult RequiresScope() =>
        Ok(new { message = $"Granted — your client's scopes cover '{DemoAuthz.ManageIdentityScope}'.", actor = Actor() });

    /// <summary>Requires both the user permission and the client scope (both dimensions together).</summary>
    [HttpGet("permission-and-scope")]
    [Authorize]
    [RequirePermission(DemoAuthz.ReadDemoPermission)]
    [RequireScope(DemoAuthz.ManageIdentityScope)]
    public IActionResult RequiresBoth() =>
        Ok(new { message = "Granted — permission AND scope satisfied.", actor = Actor() });

    private object Actor() => new
    {
        userId = actor.UserId,
        clientId = actor.ClientId,
        tenantId = actor.TenantId,
        sessionId = actor.SessionId,
    };
}

/// <summary>Authorization names this sample checks. The scope operations match the provider's seeded data.</summary>
public static class DemoAuthz
{
    /// <summary>A demo permission you provision on the provider to test the user (RBAC) dimension.</summary>
    public const string ReadDemoPermission = "demo.read";

    /// <summary>Seeded operation; the default client's <c>identity.manage</c> scope covers it.</summary>
    public const string ManageIdentityScope = "manage:identity";

    /// <summary>Seeded read operation.</summary>
    public const string ReadIdentityScope = "read:identity";
}
