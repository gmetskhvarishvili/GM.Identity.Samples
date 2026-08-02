using Asp.Versioning;
using GM.API.Controllers;
using GM.API.Models;
using GM.Identity.Sample.API.Operations;
using GM.Identity.Sample.Application.Scopes.Commands.CreateScope;
using GM.Identity.Sample.Application.Scopes.Commands.CreateScopeOperation;
using GM.Identity.Sample.Application.Scopes.Commands.DeleteScope;
using GM.Identity.Sample.Application.Scopes.Commands.DeleteScopeOperation;
using GM.Identity.Sample.Application.Scopes.Commands.UpdateScope;
using GM.Identity.Sample.Application.Scopes.Queries.GetScopeDetails;
using GM.Identity.Sample.Application.Scopes.Queries.GetScopeOperationsList;
using GM.Identity.Sample.Application.Scopes.Queries.GetScopesList;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace GM.Identity.Sample.API.Scopes;

/// <summary>
/// Scopes Controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ScopesController : BaseController
{
    /// <summary>
    /// Add Scope
    /// </summary>
    /// <param name="request">Scope Model to Add</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Scope Id</returns>
    [HttpPost(Name = nameof(AddScope))]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddScope(
        [FromBody] CreateScopeModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateScopeCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }
    
    /// <summary>
    /// Add Scope Operation
    /// </summary>
    /// <param name="id">Scope Id to Update</param>
    /// <param name="request">Scope Operation Model to Add</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Scope Id</returns>
    [HttpPost("{id}/Operations", Name = nameof(AddScopeOperation))]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddScopeOperation(
        [FromRoute] Guid id,
        [FromBody] CreateScopeOperationModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateScopeOperationCommand>();
        command.ScopeId = id;
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update Scope
    /// </summary>
    /// <param name="id">Scope Id to Update</param>
    /// <param name="request">Scope Model to Update</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpPut("{id}", Name = nameof(UpdateScope))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateScope(
        [FromRoute] Guid id,
        [FromBody] UpdateScopeModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateScopeCommand>();
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Delete Scope
    /// </summary>
    /// <param name="id">Scope Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}", Name = nameof(DeleteScope))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScope(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteScopeCommand
        {
            Id = id
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Delete Scope Operation
    /// </summary>
    /// <param name="id">Scope Id to Delete</param>
    /// <param name="operationId">Operation Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HttpDelete("{id}/Operations/{operationId}", Name = nameof(DeleteScopeOperation))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScopeOperation(
        [FromRoute] Guid id,
        [FromRoute] Guid operationId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteScopeOperationCommand
        {
            ScopeId = id,
            OperationId = operationId
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Get Scopes List
    /// </summary>
    /// <param name="request">Scope Model to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>IEnumerable of Scopes</returns>
    [HttpGet(Name = nameof(GetScopesList))]
    [ProducesResponseType(typeof(IEnumerable<ScopeModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScopesList(
        [FromQuery] GetScopesListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetScopesListQuery>();
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<ScopeModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }

    /// <summary>
    /// Get Scope Operations List
    /// </summary>
    /// <param name="id">Scope Id to Get</param>
    /// <param name="request">Scope Operation Model to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Scope Operations</returns>
    [HttpGet("{id}/Operations", Name = nameof(GetScopeOperations))]
    [ProducesResponseType(typeof(IEnumerable<OperationModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScopeOperations(
        [FromRoute] Guid id,
        [FromQuery] GetBaseListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetScopeOperationsListQuery>();
        query.ScopeId = id;
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<OperationModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }

    /// <summary>
    /// Get Scope Details
    /// </summary>
    /// <param name="id">Scope Id to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Scope Details</returns>
    [HttpGet("{id}", Name = nameof(GetScopeDetails))]
    [ProducesResponseType(typeof(ScopeDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScopeDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetScopeDetailsQuery
        {
            Id = id
        };
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Adapt<ScopeDetailsModel>();
        return Ok(result);
    }
}