using Asp.Versioning;
using GM.API.Controllers;
using GM.Identity.Sample.Application.Scopes.Commands.RestoreScope;
using GM.Identity.Sample.Application.Scopes.Commands.ActivateScope;
using GM.Identity.Sample.Application.Scopes.Commands.DeactivateScope;
using GM.Identity.Sample.Application.Scopes.Commands.HideScope;
using GM.Identity.Sample.Application.Scopes.Commands.UnhideScope;
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
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GM.API.Authorization;
using GM.Identity.Sample.Domain.SeedWork;

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
    [HasPermission(nameof(AddScope))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost(Name = nameof(AddScope))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
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
    [HasPermission(nameof(AddScopeOperation))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Operations", Name = nameof(AddScopeOperation))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
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
    [HasPermission(nameof(UpdateScope))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
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
    [HasPermission(nameof(DeleteScope))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
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
    [HasPermission(nameof(DeleteScopeOperation))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
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
    [HasPermission(nameof(GetScopesList))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
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
    [HasPermission(nameof(GetScopeOperations))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
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
    /// <param name="request">Visibility scope (query string); defaults to visible-only.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Scope Details</returns>
    [HasPermission(nameof(GetScopeDetails))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
    [HttpGet("{id}", Name = nameof(GetScopeDetails))]
    [ProducesResponseType(typeof(ScopeDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScopeDetails(
        [FromRoute] Guid id,
        [FromQuery] GetBaseDetailsModel request,
        CancellationToken cancellationToken)
    {
        var query = new GetScopeDetailsQuery
        {
            Id = id,
            Visibility = request.Visibility
        };
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Adapt<ScopeDetailsModel>();
        return Ok(result);
    }

    /// <summary>
    /// Restore Scope
    /// </summary>
    /// <param name="id">Scope Id to restore</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(RestoreScope))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Restore", Name = nameof(RestoreScope))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreScope(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new RestoreScopeCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Activate Scope
    /// </summary>
    /// <param name="id">Scope Id to activate</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(ActivateScope))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Activate", Name = nameof(ActivateScope))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateScope(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new ActivateScopeCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Deactivate Scope
    /// </summary>
    /// <param name="id">Scope Id to deactivate</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(DeactivateScope))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Deactivate", Name = nameof(DeactivateScope))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateScope(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeactivateScopeCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Hide Scope
    /// </summary>
    /// <param name="id">Scope Id to hide</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(HideScope))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Hide", Name = nameof(HideScope))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HideScope(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new HideScopeCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Unhide Scope
    /// </summary>
    /// <param name="id">Scope Id to unhide</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(UnhideScope))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Unhide", Name = nameof(UnhideScope))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnhideScope(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new UnhideScopeCommand { Id = id }, cancellationToken);
        return Ok();
    }
}
