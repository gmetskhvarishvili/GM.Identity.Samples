using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>A consent document the new user accepts at registration.</summary>
public class CreateUserConsentModel
{
    /// <summary>The consent document accepted (e.g. TermsOfService, PrivacyPolicy).</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ConsentType")]
    public string ConsentType { get; set; } = null!;

    /// <summary>The version accepted; must match the document's current version.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "DocumentVersion")]
    public string DocumentVersion { get; set; } = null!;
}

public class CreateUserConsentModelValidator : AbstractValidator<CreateUserConsentModel>
{
    public CreateUserConsentModelValidator()
    {
        RuleFor(x => x.ConsentType).NotNull().NotEmpty();
        RuleFor(x => x.DocumentVersion).NotNull().NotEmpty();
    }
}
