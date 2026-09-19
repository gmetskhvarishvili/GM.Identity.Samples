using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

public class ResetUserPasswordModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Email")]
    public string Email { get; set; } = null!;
    [Display(ResourceType = typeof(StringResource), Name = "NotificationType")]
    public int NotificationType { get; set; }
}

public class ResetUserPasswordModelValidator : AbstractValidator<ResetUserPasswordModel>
{
    public ResetUserPasswordModelValidator()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty();
        RuleFor(x => x.NotificationType).NotNull();
    }
}