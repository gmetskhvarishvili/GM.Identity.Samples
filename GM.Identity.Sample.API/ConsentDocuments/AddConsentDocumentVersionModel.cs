using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.ConsentDocuments;

/// <summary>Publish a new version of an existing consent document type. The new version becomes current.</summary>
public class AddConsentDocumentVersionModel
{
    /// <summary>The human-readable title.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Title")]
    public string? Title { get; set; }
    /// <summary>The document body (or a URL to it) for this version.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Content")]
    public string? Content { get; set; }
    /// <summary>The new version identifier (must not already exist for this type).</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Version")]
    public string? Version { get; set; }
    /// <summary>Whether an unaccepted current version is reported as pending for the user.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IsMandatory")]
    public bool IsMandatory { get; set; }
}

public class AddConsentDocumentVersionModelValidator : AbstractValidator<AddConsentDocumentVersionModel>
{
    public AddConsentDocumentVersionModelValidator()
    {
        RuleFor(x => x.Title).NotNull().NotEmpty();
        RuleFor(x => x.Content).NotNull().NotEmpty();
        RuleFor(x => x.Version).NotNull().NotEmpty();
    }
}
