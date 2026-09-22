using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.ConsentDocuments;

/// <summary>Create consent document model.</summary>
public class CreateConsentDocumentModel
{
    /// <summary>The stable document identifier (e.g. TermsOfService, PrivacyPolicy).</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ConsentType")]
    public string? ConsentType { get; set; }
    /// <summary>The human-readable title.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Title")]
    public string? Title { get; set; }
    /// <summary>The document body (or a URL to it).</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Content")]
    public string? Content { get; set; }
    /// <summary>The version users must accept.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "CurrentVersion")]
    public string? CurrentVersion { get; set; }
    /// <summary>Whether an unaccepted current version blocks login.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IsMandatory")]
    public bool IsMandatory { get; set; }
}

public class CreateConsentDocumentModelValidator : AbstractValidator<CreateConsentDocumentModel>
{
    public CreateConsentDocumentModelValidator()
    {
        RuleFor(x => x.ConsentType).NotNull().NotEmpty();
        RuleFor(x => x.Title).NotNull().NotEmpty();
        RuleFor(x => x.Content).NotNull().NotEmpty();
        RuleFor(x => x.CurrentVersion).NotNull().NotEmpty();
    }
}
