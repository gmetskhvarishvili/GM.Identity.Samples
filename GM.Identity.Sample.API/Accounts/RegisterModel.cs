using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Accounts;

/// <summary>Self-registration request body.</summary>
public class RegisterModel
{
    /// <summary>The username.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "UserName")]
    public string Username { get; set; } = null!;
    /// <summary>The email.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Email")]
    public string Email { get; set; } = null!;
    /// <summary>The phone number.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "PhoneNumber")]
    public string? PhoneNumber { get; set; }
    /// <summary>The password.</summary>
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
