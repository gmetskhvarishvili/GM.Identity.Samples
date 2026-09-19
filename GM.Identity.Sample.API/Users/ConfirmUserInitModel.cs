using FluentValidation;
using GM.Identity.Sample.Domain.Enums;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Confirm user init model.</summary>
public class ConfirmUserInitModel
{
    /// <summary>The confirmation type.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ConfirmationType")]
    public ConfirmationType ConfirmationType { get; set; }
}

public class ConfirmUserInitModelValidator : AbstractValidator<ConfirmUserInitModel>
{
    public ConfirmUserInitModelValidator()
    {
        RuleFor(x => x.ConfirmationType).IsInEnum();
    }
}
