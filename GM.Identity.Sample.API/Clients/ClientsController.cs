using Asp.Versioning;
using GM.API.Controllers;
using GM.API.Models;
using GM.Identity.Sample.API.Scopes;
using GM.Identity.Sample.Application.Clients.Commands.CreateClient;
using GM.Identity.Sample.Application.Clients.Commands.CreateClientScope;
using GM.Identity.Sample.Application.Clients.Commands.DeleteAllClientSessions;
using GM.Identity.Sample.Application.Clients.Commands.DeleteClient;
using GM.Identity.Sample.Application.Clients.Commands.DeleteClientScope;
using GM.Identity.Sample.Application.Clients.Commands.DeleteClientSession;
using GM.Identity.Sample.Application.Clients.Commands.UpdateClient;
using GM.Identity.Sample.Application.Clients.Queries.GetClientDetails;
using GM.Identity.Sample.Application.Clients.Queries.GetClientScopesList;
using GM.Identity.Sample.Application.Clients.Queries.GetClientSessionsList;
using GM.Identity.Sample.Application.Clients.Queries.GetClientsList;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace GM.Identity.Sample.API.Clients;

/// <summary>
/// Clients Controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ClientsController : BaseController
{
    /// <summary>
    /// Add Client
    /// </summary>
    /// <param name="request">Client Model to Add</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Client Id</returns>
    [HttpPost(Name = nameof(AddClient))]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddClient(
        [FromBody] CreateClientModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateClientCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }
    
    /// <summary>
    /// Add Client Scope
    /// </summary>
    /// <param name="id">Client Id to Update</param>
    /// <param name="request">Client Scope Model to Add</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpPost("{id}/Scopes", Name = nameof(AddClientScope))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddClientScope(
        [FromRoute] Guid id,
        [FromBody] CreateClientScopeModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateClientScopeCommand>();
        command.ClientId = id;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Update Client
    /// </summary>
    /// <param name="id">Client Id to Update</param>
    /// <param name="request">Client Model to Update</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpPut("{id}", Name = nameof(UpdateClient))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateClient(
        [FromRoute] Guid id,
        [FromBody] UpdateClientModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateClientCommand>();
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Delete Client
    /// </summary>
    /// <param name="id">Client Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}", Name = nameof(DeleteClient))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClient(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteClientCommand
        {
            Id = id
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }
    
    /// <summary>
    /// Delete Client Scope
    /// </summary>
    /// <param name="id">Client Id to Delete</param>
    /// <param name="scopeId">Scope Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}/Scopes/{scopeId}", Name = nameof(DeleteClientScope))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClientScope(
        [FromRoute] Guid id,
        [FromRoute] Guid scopeId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteClientScopeCommand
        {
            ClientId = id,
            ScopeId = scopeId,
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }
    
    /// <summary>
    /// Delete Client Sessions
    /// </summary>
    /// <param name="id">Client Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}/Sessions", Name = nameof(DeleteClientSessions))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClientSessions(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteAllClientSessionsCommand
        {
            ClientId = id,
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }
    
    /// <summary>
    /// Delete Client Session
    /// </summary>
    /// <param name="id">Client Id to Delete</param>
    /// <param name="sessionId">Session Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}/Sessions/{sessionId}", Name = nameof(DeleteClientSession))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClientSession(
        [FromRoute] Guid id,
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteClientSessionCommand
        {
            ClientId = id,
            Id = sessionId,
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Get Clients List
    /// </summary>
    /// <param name="request">Client Model to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>IEnumerable of Roles</returns>
    [HttpGet(Name = nameof(GetClientsList))]
    [ProducesResponseType(typeof(IEnumerable<ClientModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClientsList(
        [FromQuery] GetClientsListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetClientsListQuery>();
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<ClientModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }
    
    /// <summary>
    /// Get Client Scopes List
    /// </summary>
    /// <param name="id">Client Id to Get</param>
    /// <param name="request">Client Scope Model to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Client Scopes</returns>
    [HttpGet("{id}/Scopes", Name = nameof(GetClientScopes))]
    [ProducesResponseType(typeof(IEnumerable<ClientModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClientScopes(
        [FromRoute] Guid id,
        [FromQuery] GetBaseListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetClientScopesListQuery>();
        query.ClientId = id;
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<ScopeModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }
    
    /// <summary>
    /// Get Client Sessions List
    /// </summary>
    /// <param name="id">Client Id to Get</param>
    /// <param name="request">Client Session Model to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Client Sessions</returns>
    [HttpGet("{id}/Sessions", Name = nameof(GetClientSessions))]
    [ProducesResponseType(typeof(IEnumerable<ClientSessionModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClientSessions(
        [FromRoute] Guid id,
        [FromQuery] GetClientSessionsListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetClientSessionsListQuery>();
        query.ClientId = id;
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<ClientSessionModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }

    /// <summary>
    /// Get Client Details
    /// </summary>
    /// <param name="id">Client Id to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Client Details</returns>
    [HttpGet("{id}", Name = nameof(GetClientDetails))]
    [ProducesResponseType(typeof(ClientDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClientDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetClientDetailsQuery
        {
            Id = id
        };
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Adapt<ClientDetailsModel>();
        return Ok(result);
    }
}