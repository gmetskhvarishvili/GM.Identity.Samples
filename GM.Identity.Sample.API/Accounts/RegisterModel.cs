using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Accounts;

/// <summary>Self-registration request body.</summary>
public class RegisterModel
{
    [Display(ResourceType = typeof(StringResource), Name = "UserName")]
    public string Username { get; set; } = null!;
    [Display(ResourceType = typeof(StringResource), Name = "Email")]
    public string Email { get; set; } = null!;
    [Display(ResourceType = typeof(StringResource), Name = "PhoneNumber")]
    public string? PhoneNumber { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "Password")]
    public string Password { get; set; } = null!;
}

public class RegisterModelValidator : AbstractValidator<RegisterModel>
{
    public RegisterModelValidator()
    {
        RuleFor(x => x.Username).NotNull().NotEmpty();
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}
