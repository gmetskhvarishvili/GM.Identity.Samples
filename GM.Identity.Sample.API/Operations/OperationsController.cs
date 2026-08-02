using Asp.Versioning;
using GM.API.Controllers;
using GM.Identity.Sample.Application.Operations.Commands.CreateOperation;
using GM.Identity.Sample.Application.Operations.Commands.DeleteOperation;
using GM.Identity.Sample.Application.Operations.Commands.UpdateOperation;
using GM.Identity.Sample.Application.Operations.Queries.GetOperationDetails;
using GM.Identity.Sample.Application.Operations.Queries.GetOperationsList;
using Mapster;
using Microsoft.AspNetCore.Mvc;

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
    [HttpPost(Name = nameof(AddOperation))]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
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
    /// <param name="cancellationToken"></param>
    /// <returns>Operation Details</returns>
    [HttpGet("{id}", Name = nameof(GetOperationDetails))]
    [ProducesResponseType(typeof(OperationDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOperationDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetOperationDetailsQuery
        {
            Id = id
        };
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Adapt<OperationDetailsModel>();
        return Ok(result);
    }
}