using System.Diagnostics.CodeAnalysis;
using GM.API.Controllers;
using GM.Identity.Sample.Application.Accounts.Commands.Authorize;
using GM.Identity.Sample.Application.Accounts.Commands.ExternalAuthorize;
using GM.Identity.Sample.Application.Accounts.Commands.IntrospectToken;
using GM.Identity.Sample.Application.Accounts.Commands.RevokeToken;
using GM.Identity.Sample.Application.Accounts.Queries.GetOAuthRedirectUri;
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
        };
        var result = await Mediator.Send(command, cancellationToken);
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
