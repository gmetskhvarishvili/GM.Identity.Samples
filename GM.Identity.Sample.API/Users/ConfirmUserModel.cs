using FluentValidation;
using GM.Identity.Sample.Domain.Enums;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Confirm user model.</summary>
public class ConfirmUserModel
{
    /// <summary>The code.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Code")]
    public string Code { get; set; } = null!;
    /// <summary>The confirmation type.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ConfirmationType")]
    public ConfirmationType ConfirmationType { get; set; }
}

public class ConfirmUserModelValidator : AbstractValidator<ConfirmUserModel>
{
    public ConfirmUserModelValidator()
    {
        RuleFor(x => x.Code).NotNull().NotEmpty();
        RuleFor(x => x.ConfirmationType).IsInEnum();
    }
}
