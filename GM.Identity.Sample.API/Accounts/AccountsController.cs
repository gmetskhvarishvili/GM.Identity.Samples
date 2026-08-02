using GM.API.Controllers;
using GM.Identity.Sample.Application.Accounts.Commands.Authorize;
using GM.Identity.Sample.Application.Accounts.Commands.ExternalAuthorize;
using GM.Identity.Sample.Application.Accounts.Queries.GetOAuthRedirectUri;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GM.Identity.Sample.API.Accounts;

/// <summary>
/// Accounts Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
        };
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
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