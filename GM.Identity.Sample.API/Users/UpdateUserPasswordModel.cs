using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

public class UpdateUserPasswordModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Password")]
    public string Password { get; set; } = null!;
}

public class UpdateUserPasswordModelValidator : AbstractValidator<UpdateUserPasswordModel>
{
    public UpdateUserPasswordModelValidator()
    {
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}