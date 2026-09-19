using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Body to record acceptance of a consent document.</summary>
public class RecordConsentModel
{
    /// <summary>The consent type.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ConsentType")]
    public string ConsentType { get; set; } = null!;
    /// <summary>The document version.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "DocumentVersion")]
    public string DocumentVersion { get; set; } = null!;
}

public class RecordConsentModelValidator : AbstractValidator<RecordConsentModel>
{
    public RecordConsentModelValidator()
    {
        RuleFor(x => x.ConsentType).NotNull().NotEmpty();
        RuleFor(x => x.DocumentVersion).NotNull().NotEmpty();
    }
}
