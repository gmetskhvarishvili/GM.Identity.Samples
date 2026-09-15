using System.Diagnostics.CodeAnalysis;
using GM.API.Controllers;
using GM.Identity.Sample.Application.Accounts.Commands.Authorize;
using GM.Identity.Sample.Application.Accounts.Commands.AuthorizeCode;
using GM.Identity.Sample.Application.Accounts.Commands.BeginPasskeyAssertion;
using GM.Identity.Sample.Application.Accounts.Commands.CompletePasskeyAssertion;
using GM.Identity.Sample.Application.Accounts.Commands.ExternalAuthorize;
using GM.Identity.Sample.Application.Accounts.Commands.IntrospectToken;
using GM.Identity.Sample.Application.Accounts.Commands.RegisterUser;
using GM.Identity.Sample.Application.Accounts.Commands.RevokeToken;
using GM.Identity.Sample.Application.Accounts.Queries.GetOAuthRedirectUri;
using GM.Identity.Sample.Application.Accounts.Queries.GetOpenIdConfiguration;
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
        }, cancellationToken);
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
}
