using Asp.Versioning;
using GM.API.Authorization;
using GM.API.Controllers;
using GM.API.Models;
using GM.Identity.Sample.Application.ConsentDocuments.Commands.AddConsentDocumentVersion;
using GM.Identity.Sample.Application.ConsentDocuments.Commands.CreateConsentDocument;
using GM.Identity.Sample.Application.ConsentDocuments.Commands.DeleteConsentDocument;
using GM.Identity.Sample.Application.ConsentDocuments.Queries.GetConsentDocumentDetails;
using GM.Identity.Sample.Application.ConsentDocuments.Queries.GetConsentDocumentsList;
using GM.Identity.Sample.Domain.SeedWork;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.API.ConsentDocuments;

/// <summary>
/// Consent Documents Controller — admin CRUD for the registry of documents (Terms of Service, Privacy Policy, …)
/// that users must accept. A mandatory document whose current version is unaccepted blocks the user's login.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ConsentDocumentsController : BaseController
{
    /// <summary>
    /// Add Consent Document
    /// </summary>
    /// <param name="request">Consent document model to add</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Consent document Id</returns>
    [HasPermission(nameof(AddConsentDocument))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost(Name = nameof(AddConsentDocument))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddConsentDocument(
        [FromBody] CreateConsentDocumentModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateConsentDocumentCommand>();
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Add Consent Document Version
    /// </summary>
    /// <param name="consentType">The document type to publish a new version of</param>
    /// <param name="request">The new version to publish (becomes current)</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The new version's Id</returns>
    [HasPermission(nameof(AddConsentDocumentVersion))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPost("{consentType}/Versions", Name = nameof(AddConsentDocumentVersion))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddConsentDocumentVersion(
        [FromRoute] string consentType,
        [FromBody] AddConsentDocumentVersionModel request,
        CancellationToken cancellationToken)
    {
        var command = request.Adapt<AddConsentDocumentVersionCommand>();
        command.ConsentType = consentType;
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete Consent Document
    /// </summary>
    /// <param name="id">Consent document Id to delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Result</returns>
    [HasPermission(nameof(DeleteConsentDocument))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpDelete("{id}", Name = nameof(DeleteConsentDocument))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConsentDocument(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteConsentDocumentCommand { Id = id }, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Get Consent Documents List
    /// </summary>
    /// <param name="request">Consent documents list model to get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>IEnumerable of consent documents</returns>
    [HasPermission(nameof(GetConsentDocumentsList))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
    [HttpGet(Name = nameof(GetConsentDocumentsList))]
    [ProducesResponseType(typeof(IEnumerable<ConsentDocumentModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConsentDocumentsList(
        [FromQuery] GetConsentDocumentsListModel request,
        CancellationToken cancellationToken)
    {
        var query = request.Adapt<GetConsentDocumentsListQuery>();
        var response = await Mediator.Send(query, cancellationToken);
        var result = response.Items.Adapt<IEnumerable<ConsentDocumentModel>>();
        AddPaginationHeader(response.TotalCount, response.PageSize, response.CurrentPage, response.TotalPages);
        return Ok(result);
    }

    /// <summary>
    /// Get Consent Document Details
    /// </summary>
    /// <param name="id">Consent document Id to get</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Consent document details</returns>
    [HasPermission(nameof(GetConsentDocumentDetails))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
    [HttpGet("{id}", Name = nameof(GetConsentDocumentDetails))]
    [ProducesResponseType(typeof(ConsentDocumentDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsentDocumentDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new GetConsentDocumentDetailsQuery { Id = id }, cancellationToken);
        var result = response.Adapt<ConsentDocumentDetailsModel>();
        return Ok(result);
    }
}
