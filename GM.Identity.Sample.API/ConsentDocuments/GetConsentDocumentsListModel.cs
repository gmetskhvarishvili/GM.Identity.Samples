using FluentValidation;
using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.ConsentDocuments;

/// <summary>Get consent documents list model.</summary>
public class GetConsentDocumentsListModel : GetBaseListModel
{
    /// <summary>The id of the consent document.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid? Id { get; set; }

    /// <summary>The consent type filter.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ConsentType")]
    public string? ConsentType { get; set; }

    /// <summary>Filter to mandatory (or non-mandatory) documents.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IsMandatory")]
    public bool? IsMandatory { get; set; }
}

/// <inheritdoc />
public class GetConsentDocumentsListModelValidator : AbstractValidator<GetConsentDocumentsListModel>
{
    /// <inheritdoc />
    public GetConsentDocumentsListModelValidator()
    {
    }
}
