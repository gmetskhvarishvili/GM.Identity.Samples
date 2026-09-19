using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

public class ChangeCurrentUserPasswordModel
{
    [Display(ResourceType = typeof(StringResource), Name = "CurrentPassword")]
    public string CurrentPassword { get; set; } = null!;
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
