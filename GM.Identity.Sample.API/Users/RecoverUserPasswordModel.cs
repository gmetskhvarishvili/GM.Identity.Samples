using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

public class RecoverUserPasswordModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Email")]
    public string Email { get; set; } = null!;
    [Display(ResourceType = typeof(StringResource), Name = "Code")]
    public string Code { get; set; } = null!;
    [Display(ResourceType = typeof(StringResource), Name = "Password")]
    public string Password { get; set; } = null!;
}

public class RecoverUserPasswordModelValidator : AbstractValidator<RecoverUserPasswordModel>
{
    public RecoverUserPasswordModelValidator()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty();
        RuleFor(x => x.Code).NotNull().NotEmpty();
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}