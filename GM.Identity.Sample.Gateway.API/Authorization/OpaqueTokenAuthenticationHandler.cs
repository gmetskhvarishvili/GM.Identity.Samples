using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
namespace GM.Identity.Sample.Gateway.API.Authorization;

/// <summary>
/// Validates an opaque bearer token against the Redis session store (written by the Identity API on
/// login, keyed by the token hash â€” <c>gm-identity:auth:session:{hash}</c>). On success the principal
/// carries <see cref="ClaimTypes.NameIdentifier"/> = userId and a <c>session_id</c> claim, which
/// GM.Gateway's actor forwarding turns into the trusted <c>X-User-Id</c> / <c>X-Session-Id</c> headers
/// the backend reads. No token yields no identity (anonymous routes still pass); an unknown or expired
/// token fails authentication, so a protected route (AuthorizationPolicy) returns 401.
/// </summary>
public sealed class OpaqueTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Bearer";
    private const string SessionKeyPrefix = "gm-identity:auth:session:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;

    public OpaqueTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConnectionMultiplexer redis) : base(options, logger, encoder)
    {
        _redis = redis;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) ||
            !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = header["Bearer ".Length..].Trim();
        if (token.Length == 0) return AuthenticateResult.NoResult();

        var key = SessionKeyPrefix + TokenGenerator.Hash(token);
        var json = await _redis.GetDatabase().StringGetAsync(key);
        if (json.IsNullOrEmpty) return AuthenticateResult.Fail("Unknown or expired token.");

        SessionInfo? info;
        try { info = JsonSerializer.Deserialize<SessionInfo>((string)json!, JsonOptions); }
        catch (JsonException) { return AuthenticateResult.Fail("Malformed session."); }

        if (info is null || info.ExpiresAt <= DateTime.UtcNow)
            return AuthenticateResult.Fail("Unknown or expired token.");

        var claims = new List<Claim> { new("session_id", info.SessionId.ToString()) };
        if (info.UserId is { } userId)          // user session
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        if (info.ClientId is { } clientId)      // present on both user and client sessions
            claims.Add(new Claim("client_id", clientId.ToString()));

        // Header-resolved multi-tenancy: the caller asserts the tenant via X-Tenant-Id. We normalize it
        // into a trusted "tenant_id" claim so GM.Gateway's actor forwarding re-emits it as X-Tenant-Id
        // (the raw client-supplied copy is stripped first — the gateway is the sole authority). The
        // backend's ICurrentActor.TenantId then drives the tenant query filter.
        if (Guid.TryParse(Request.Headers["X-Tenant-Id"].ToString(), out var tenantId))
            claims.Add(new Claim("tenant_id", tenantId.ToString()));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}

/// <summary>Session data read back from Redis. Mirrors the Identity API's <c>SessionInfo</c> (web/camelCase JSON).</summary>
public sealed record SessionInfo(Guid? UserId, Guid SessionId, Guid? ClientId, DateTime ExpiresAt);
