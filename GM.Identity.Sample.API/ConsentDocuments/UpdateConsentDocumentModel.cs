using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.ConsentDocuments;

/// <summary>Update consent document model (the ConsentType is immutable and set at creation).</summary>
public class UpdateConsentDocumentModel
{
    /// <summary>The human-readable title.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Title")]
    public string? Title { get; set; }
    /// <summary>The document body (or a URL to it).</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Content")]
    public string? Content { get; set; }
    /// <summary>The version users must accept. Bumping it makes prior acceptances outstanding again.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "CurrentVersion")]
    public string? CurrentVersion { get; set; }
    /// <summary>Whether an unaccepted current version blocks login.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IsMandatory")]
    public bool IsMandatory { get; set; }
}

public class UpdateConsentDocumentModelValidator : AbstractValidator<UpdateConsentDocumentModel>
{
    public UpdateConsentDocumentModelValidator()
    {
        RuleFor(x => x.Title).NotNull().NotEmpty();
        RuleFor(x => x.Content).NotNull().NotEmpty();
        RuleFor(x => x.CurrentVersion).NotNull().NotEmpty();
    }
}
