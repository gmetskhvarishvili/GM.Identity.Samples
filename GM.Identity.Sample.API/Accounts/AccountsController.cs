using System.Diagnostics.CodeAnalysis;
using GM.API.Controllers;
using GM.Identity.Sample.Application.Accounts.Commands.Authorize;
using GM.Identity.Sample.Application.Accounts.Commands.AuthorizeCode;
using GM.Identity.Sample.Application.Accounts.Commands.BeginPasskeyAssertion;
using GM.Identity.Sample.Application.Accounts.Commands.CompletePasskeyAssertion;
using GM.Identity.Sample.Application.Accounts.Commands.EndSession;
using GM.Identity.Sample.Application.Accounts.Commands.ExternalAuthorize;
using GM.Identity.Sample.Application.Accounts.Commands.IntrospectToken;
using GM.Identity.Sample.Application.Accounts.Commands.RegisterUser;
using GM.Identity.Sample.Application.Accounts.Commands.RevokeToken;
using GM.Identity.Sample.Application.Accounts.Queries.GetJwks;
using GM.Identity.Sample.Application.Accounts.Queries.GetOAuthRedirectUri;
using GM.Identity.Sample.Application.Accounts.Queries.GetOpenIdConfiguration;
using GM.Identity.Sample.Application.Infrastructure.Services.Logout;
using GM.Identity.Sample.Application.Accounts.Queries.GetUserInfo;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.API.Accounts;

/// <summary>
/// Accounts Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[SuppressMessage("Major Code Smell", "S6931:Controller actions should not use routes starting with '/'",
    Justification = "These endpoints deliberately sit at the well-known OpenID Connect paths " +
                     "(~/connect/token, ~/connect/google, ...); rewriting them as controller-relative " +
                     "routes would move them off the standard locations clients expect.")]
public class AccountsController : BaseController
{
    /// <summary>
    /// Authorize
    /// </summary>
    /// <param name="request">request</param>
    /// <param name="cancellationToken"></param>
    /// <returns>UserSession Response</returns>
    [Consumes("application/x-www-form-urlencoded")]
    [HttpPost("~/connect/token", Name = nameof(Authorize)), Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Authorize(
        AuthorizeModel request,
        CancellationToken cancellationToken)
    {
        var command = new AuthorizeCommand
        {
            UserName = request.UserName,
            Password = request.Password,
            ClientId = request.ClientId,
            ClientSecret = request.ClientSecret,
            GrantType = request.GrantType,
            RefreshToken = request.RefreshToken,
            Code = request.Code,
            RedirectUri = request.RedirectUri,
            CodeVerifier = request.CodeVerifier,
            ApiKey = request.ApiKey,
        };
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Public self-registration: creates a new account and sends an email confirmation code. Returns the new
    /// user id. The account must confirm its email via the confirm flow.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("~/register", Name = nameof(Register)), Produces("application/json")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterModel request,
        CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(new RegisterUserCommand
        {
            Username = request.Username,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Password = request.Password,
        }, cancellationToken);
        return Ok(id);
    }

    /// <summary>Begin a passkey login: returns a single-use challenge for the user's authenticator to sign.</summary>
    [AllowAnonymous]
    [HttpPost("~/connect/passkey/begin", Name = nameof(BeginPasskeyAssertion)), Produces("application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> BeginPasskeyAssertion(
        [FromBody] BeginPasskeyModel request, CancellationToken cancellationToken)
    {
        var challenge = await Mediator.Send(new BeginPasskeyAssertionCommand { UserName = request.UserName }, cancellationToken);
        return Ok(new { challenge });
    }

    /// <summary>Complete a passkey login: verifies the assertion and returns tokens.</summary>
    [AllowAnonymous]
    [HttpPost("~/connect/passkey/complete", Name = nameof(CompletePasskeyAssertion)), Produces("application/json")]
    [ProducesResponseType(typeof(PasskeyTokenDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompletePasskeyAssertion(
        [FromBody] CompletePasskeyModel request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CompletePasskeyAssertionCommand
        {
            UserName = request.UserName,
            ClientId = request.ClientId,
            CredentialId = request.CredentialId,
            AuthenticatorDataBase64Url = request.AuthenticatorData,
            ClientDataJsonBase64Url = request.ClientDataJson,
            SignatureBase64Url = request.Signature,
        }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// OpenID Connect discovery document. Advertises this provider's endpoints and supported grants/scopes so
    /// clients can configure themselves. Tokens are opaque, so no JWKS/signing metadata is advertised.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("~/.well-known/openid-configuration", Name = nameof(OpenIdConfiguration)), Produces("application/json")]
    [ProducesResponseType(typeof(OpenIdConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> OpenIdConfiguration(CancellationToken cancellationToken)
    {
        var issuer = $"{Request.Scheme}://{Request.Host}";
        var result = await Mediator.Send(new GetOpenIdConfigurationQuery(issuer), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// JSON Web Key Set: the OP's public signing keys, used by relying parties to verify back-channel logout
    /// tokens (and, in future, id_tokens). Advertised as <c>jwks_uri</c> in the discovery document.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("~/.well-known/jwks.json", Name = nameof(Jwks)), Produces("application/json")]
    [ProducesResponseType(typeof(JsonWebKeySetDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Jwks(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetJwksQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Authorization endpoint of the PKCE authorization-code flow. Authenticates the resource owner, validates
    /// the client + registered redirect URI + PKCE challenge, and returns a short-lived authorization code plus
    /// the redirect target (code + state). Exchange it at /connect/token with grant_type=authorization_code.
    /// </summary>
    [Consumes("application/x-www-form-urlencoded")]
    [HttpPost("~/connect/authorize", Name = nameof(AuthorizeCode)), Produces("application/json")]
    [ProducesResponseType(typeof(AuthorizeCodeResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AuthorizeCode(
        AuthorizeCodeModel request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new AuthorizeCodeCommand
        {
            ClientId = request.ClientId,
            RedirectUri = request.RedirectUri,
            Scope = request.Scope,
            State = request.State,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            UserName = request.UserName,
            Password = request.Password,
            Prompt = request.Prompt,
            // The browser's SSO cookie (if any) enables silent authorization for a second, third, … client.
            SsoCookie = Request.Cookies[SsoCookieName],
        }, cancellationToken);

        // A fresh interactive login establishes a new SSO session — drop its cookie so subsequent client
        // authorizations in this browser can be satisfied silently.
        if (!string.IsNullOrEmpty(result.SsoCookie))
            Response.Cookies.Append(SsoCookieName, result.SsoCookie, BuildSsoCookieOptions(result.SsoCookieExpiresAt));

        return Ok(result);
    }

    /// <summary>
    /// OpenID Connect end-session (logout) endpoint. Ends the browser's SSO session and performs Single Logout:
    /// every application session established through it is revoked at once. Clears the SSO cookie and, when a
    /// registered <c>post_logout_redirect_uri</c> is supplied, returns the redirect target.
    /// </summary>
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    [HttpPost("~/connect/endsession", Name = nameof(EndSession)), Produces("application/json")]
    [ProducesResponseType(typeof(EndSessionResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EndSession(
        EndSessionModel request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new EndSessionCommand
        {
            SsoCookie = Request.Cookies[SsoCookieName],
            ClientId = request.ClientId,
            PostLogoutRedirectUri = request.PostLogoutRedirectUri,
            State = request.State,
            Issuer = $"{Request.Scheme}://{Request.Host}",
        }, cancellationToken);

        // The SSO session is gone — remove its cookie from the browser.
        Response.Cookies.Delete(SsoCookieName, BuildSsoCookieOptions(null));

        return Ok(result);
    }

    /// <summary>
    /// Revoke an opaque access or refresh token (OAuth token revocation, RFC 7009).
    /// </summary>
    [Consumes("application/x-www-form-urlencoded")]
    [HttpPost("~/connect/revoke", Name = nameof(RevokeToken)), Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeToken(
        RevokeTokenModel request,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new RevokeTokenCommand
        {
            Token = request.Token,
            ClientId = request.ClientId,
            ClientSecret = request.ClientSecret,
        }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// OAuth 2.0 token introspection (RFC 7662). The calling client authenticates and learns whether a token
    /// is active plus its metadata; an inactive/unknown token returns <c>{ "active": false }</c>.
    /// </summary>
    [Consumes("application/x-www-form-urlencoded")]
    [HttpPost("~/connect/introspect", Name = nameof(Introspect)), Produces("application/json")]
    [ProducesResponseType(typeof(IntrospectionResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Introspect(
        IntrospectModel request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new IntrospectTokenCommand
        {
            Token = request.Token,
            ClientId = request.ClientId,
            ClientSecret = request.ClientSecret,
        }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// OpenID Connect userinfo: returns the standard claims of the user represented by the bearer access token
    /// in the Authorization header. Answers 401 when the token is missing, invalid, expired, or not a user token.
    /// </summary>
    [HttpGet("~/connect/userinfo", Name = nameof(UserInfo)), Produces("application/json")]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UserInfo(CancellationToken cancellationToken)
    {
        var authorization = Request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";
        var accessToken = authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[bearerPrefix.Length..].Trim()
            : null;

        var result = await Mediator.Send(new GetUserInfoQuery { AccessToken = accessToken }, cancellationToken);
        return result is null ? Unauthorized() : Ok(result);
    }

    /// <summary>
    /// Google Connect
    /// </summary>
    /// <param name="request">Auth Connect Request Params</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Redirect Uri</returns>
    [HttpGet("~/connect/google", Name = nameof(GoogleConnect))]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GoogleConnect([FromQuery] AuthConnectModel request, CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetOAuthRedirectUriQuery>();
        query.Provider = "Google";
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Google Connect UserSession
    /// </summary>
    /// <param name="request">Auth Connect UserSession Request Params</param>
    /// <param name="cancellationToken"></param>
    /// <returns>UserSession Response</returns>
    [HttpPost("~/connect/google/token", Name = nameof(GoogleAuthorize))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GoogleAuthorize([FromBody] ExternalAuthorizeModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<ExternalAuthorizeCommand>();
        command.Provider = "Google";
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Facebook Connect
    /// </summary>
    /// <param name="request">Auth Connect Request Params</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Redirect Uri</returns>
    [HttpGet("~/connect/facebook", Name = nameof(FacebookConnect))]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> FacebookConnect([FromQuery] AuthConnectModel request, CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetOAuthRedirectUriQuery>();
        query.Provider = "Facebook";
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Facebook Connect UserSession
    /// </summary>
    /// <param name="request">Auth Connect UserSession Request Params</param>
    /// <param name="cancellationToken"></param>
    /// <returns>UserSession Response</returns>
    [HttpPost("~/connect/facebook/token", Name = nameof(FacebookAuthorize))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> FacebookAuthorize(
        [FromBody] ExternalAuthorizeModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<ExternalAuthorizeCommand>();
        command.Provider = "Facebook";
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Generic external-login redirect for any configured provider — Microsoft, Apple, LinkedIn, GitHub, or an
    /// enterprise OpenID Connect IdP (the <c>provider</c> route segment is the configuration key under <c>OAuth</c>).
    /// Returns the provider's authorization URL to send the user agent to. Google/Facebook also have named
    /// endpoints above for backward compatibility, but work here too.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("~/connect/external/{provider}", Name = nameof(ExternalConnect))]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExternalConnect(
        string provider, [FromQuery] AuthConnectModel request, CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetOAuthRedirectUriQuery>();
        query.Provider = provider;
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Generic external-login callback for any configured provider: exchanges the authorization code, resolves
    /// the user's email (id_token for OIDC providers, userinfo for OAuth2 ones), provisions or matches the local
    /// account, and issues a session.
    /// </summary>
    [HttpPost("~/connect/external/{provider}/token", Name = nameof(ExternalAuthorizeConnect))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExternalAuthorizeConnect(
        string provider, [FromBody] ExternalAuthorizeModel request, CancellationToken cancellationToken)
    {
        var command = request.Adapt<ExternalAuthorizeCommand>();
        command.Provider = provider;
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Name of the browser cookie that carries the opaque single sign-on session value.</summary>
    private const string SsoCookieName = "gm_sso";

    // Hardened options for the SSO cookie: not script-readable, HTTPS-only, and Lax so it survives the
    // top-level redirects of the authorization-code flow while resisting cross-site use. Path-scoped to the
    // OAuth endpoints. The expiry (when set) matches the SSO session's; delete passes null for immediate removal.
    private static CookieOptions BuildSsoCookieOptions(DateTime? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/connect",
        Expires = expiresAt.HasValue ? new DateTimeOffset(expiresAt.Value, TimeSpan.Zero) : null,
    };
}
