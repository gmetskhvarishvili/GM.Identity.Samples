using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Change current user password model.</summary>
public class ChangeCurrentUserPasswordModel
{
    /// <summary>The current password.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "CurrentPassword")]
    public string CurrentPassword { get; set; } = null!;
    /// <summary>The new password.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "NewPassword")]
    public string NewPassword { get; set; } = null!;
}

public class ChangeCurrentUserPasswordModelValidator : AbstractValidator<ChangeCurrentUserPasswordModel>
{
    public ChangeCurrentUserPasswordModelValidator()
    {
        RuleFor(x => x.CurrentPassword).NotNull().NotEmpty();
        RuleFor(x => x.NewPassword).NotNull().NotEmpty();
    }
}
