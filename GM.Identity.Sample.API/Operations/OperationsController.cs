using Asp.Versioning;
using GM.API.Controllers;
using GM.Identity.Sample.Application.Operations.Commands.RestoreOperation;
using GM.Identity.Sample.Application.Operations.Commands.ActivateOperation;
using GM.Identity.Sample.Application.Operations.Commands.DeactivateOperation;
using GM.Identity.Sample.Application.Operations.Commands.HideOperation;
using GM.Identity.Sample.Application.Operations.Commands.UnhideOperation;
using GM.API.Models;
using GM.Identity.Sample.Application.Operations.Commands.CreateOperation;
using GM.Identity.Sample.Application.Operations.Commands.DeleteOperation;
using GM.Identity.Sample.Application.Operations.Commands.UpdateOperation;
using GM.Identity.Sample.Application.Operations.Queries.GetOperationDetails;
using GM.Identity.Sample.Application.Operations.Queries.GetOperationsList;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GM.API.Authorization;
using GM.Identity.Sample.Domain.SeedWork;

namespace GM.Identity.Sample.API.Operations;

/// <summary>
/// Operations Controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OperationsController : BaseController
{
    /// <summary>
    /// Add Operation
    /// </summary>
    /// <param name="request">Operation Model to Add</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Operation Id</returns>
    [HasPermission(nameof(AddOperation))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost(Name = nameof(AddOperation))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddOperation(
        [FromBody] CreateOperationModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateOperationCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update Operation
    /// </summary>
    /// <param name="id">Operation Id to Update</param>
    /// <param name="request">Operation Model to Update</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(UpdateOperation))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPut("{id}", Name = nameof(UpdateOperation))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOperation(
        [FromRoute] Guid id,
        [FromBody] UpdateOperationModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateOperationCommand>();
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Delete Operation
    /// </summary>
    /// <param name="id">Operation Id to Delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(DeleteOperation))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpDelete("{id}", Name = nameof(DeleteOperation))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOperation(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteOperationCommand
        {
            Id = id
        };
        await Mediator.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Get Operations List
    /// </summary>
    /// <param name="request">Operation Model to Get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>IEnumerable of Operations</returns>
    [HasPermission(nameof(GetOperationsList))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
    [HttpGet(Name = nameof(GetOperationsList))]
    [ProducesResponseType(typeof(IEnumerable<OperationModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOperationsList(
        [FromQuery] GetOperationsListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetOperationsListQuery>();
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<OperationModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }

    /// <summary>
    /// Get Operation Details
    /// </summary>
    /// <param name="id">Operation Id to Get</param>
    /// <param name="request">Visibility scope (query string); defaults to visible-only.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Operation Details</returns>
    [HasPermission(nameof(GetOperationDetails))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
    [HttpGet("{id}", Name = nameof(GetOperationDetails))]
    [ProducesResponseType(typeof(OperationDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOperationDetails(
        [FromRoute] Guid id,
        [FromQuery] GetBaseDetailsModel request,
        CancellationToken cancellationToken)
    {
        var query = new GetOperationDetailsQuery
        {
            Id = id,
            Visibility = request.Visibility
        };
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Adapt<OperationDetailsModel>();
        return Ok(result);
    }

    /// <summary>
    /// Restore Operation
    /// </summary>
    /// <param name="id">Operation Id to restore</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(RestoreOperation))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Restore", Name = nameof(RestoreOperation))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreOperation(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new RestoreOperationCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Activate Operation
    /// </summary>
    /// <param name="id">Operation Id to activate</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(ActivateOperation))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Activate", Name = nameof(ActivateOperation))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateOperation(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new ActivateOperationCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Deactivate Operation
    /// </summary>
    /// <param name="id">Operation Id to deactivate</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(DeactivateOperation))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Deactivate", Name = nameof(DeactivateOperation))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateOperation(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeactivateOperationCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Hide Operation
    /// </summary>
    /// <param name="id">Operation Id to hide</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(HideOperation))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Hide", Name = nameof(HideOperation))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HideOperation(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new HideOperationCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Unhide Operation
    /// </summary>
    /// <param name="id">Operation Id to unhide</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(UnhideOperation))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{id}/Unhide", Name = nameof(UnhideOperation))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnhideOperation(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new UnhideOperationCommand { Id = id }, cancellationToken);
        return Ok();
    }
}
