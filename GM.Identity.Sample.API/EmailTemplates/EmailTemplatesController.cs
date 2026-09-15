using Asp.Versioning;
using FluentValidation;
using GM.API.Authorization;
using GM.API.Controllers;
using GM.Identity.Sample.Application.EmailTemplates.Commands.SaveEmailTemplate;
using GM.Identity.Sample.Application.EmailTemplates.Queries.GetEmailTemplates;
using GM.Identity.Sample.Domain.SeedWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.API.EmailTemplates;

/// <summary>Email Templates Controller — manage the editable email templates (subject/body by key).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EmailTemplatesController : BaseController
{
    /// <summary>Create or update an email template (upsert by key).</summary>
    [HasPermission(nameof(SaveEmailTemplate))]
    [RequiresScope(ScopeOperations.ManageIdentity)]
    [HttpPut("{key}", Name = nameof(SaveEmailTemplate))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveEmailTemplate(
        [FromRoute] string key, [FromBody] SaveEmailTemplateModel request, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(new SaveEmailTemplateCommand
        {
            Key = key,
            Subject = request.Subject,
            Body = request.Body,
        }, cancellationToken);
        return Ok(id);
    }

    /// <summary>List all email templates.</summary>
    [HasPermission(nameof(GetEmailTemplates))]
    [RequiresScope(ScopeOperations.ReadIdentity)]
    [HttpGet(Name = nameof(GetEmailTemplates))]
    [ProducesResponseType(typeof(IReadOnlyList<EmailTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmailTemplates(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetEmailTemplatesQuery(), cancellationToken);
        return Ok(result);
    }
}

/// <summary>Body for creating/updating an email template.</summary>
public class SaveEmailTemplateModel
{
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
}

public class SaveEmailTemplateModelValidator : AbstractValidator<SaveEmailTemplateModel>
{
    public SaveEmailTemplateModelValidator()
    {
        RuleFor(x => x.Subject).NotNull().NotEmpty();
        RuleFor(x => x.Body).NotNull().NotEmpty();
    }
}
