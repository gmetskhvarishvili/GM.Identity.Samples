using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GM.Identity.Sample.Service.API.Authorization;

/// <summary>
/// Validates the Bearer access token by calling the Identity provider's RFC 7662 introspection endpoint over
/// HTTP (rather than reading the shared session cache). On an active token the principal carries the
/// provider-reported <see cref="ClaimTypes.NameIdentifier"/> (userId), <c>client_id</c> and <c>scope</c>.
/// No token / not a Bearer header → no result; an inactive or unknown token → failure.
/// </summary>
public sealed class IntrospectionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Bearer";

    private readonly IIdentityProviderClient _provider;

    public IntrospectionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IIdentityProviderClient provider) : base(options, logger, encoder)
    {
        _provider = provider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) ||
            !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = header["Bearer ".Length..].Trim();
        if (token.Length == 0) return AuthenticateResult.NoResult();

        var result = await _provider.IntrospectAsync(token, Context.RequestAborted);
        if (!result.Active) return AuthenticateResult.Fail("Inactive or unknown token.");

        var claims = new List<Claim>();
        if (Guid.TryParse(result.Sub, out _)) claims.Add(new Claim(ClaimTypes.NameIdentifier, result.Sub!));
        if (Guid.TryParse(result.ClientId, out _)) claims.Add(new Claim("client_id", result.ClientId!));
        if (!string.IsNullOrWhiteSpace(result.Scope)) claims.Add(new Claim("scope", result.Scope!));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
