using FluentValidation;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Create user model.</summary>
public class CreateUserModel
{
    /// <summary>The username.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "UserName")]
    public string? Username { get; set; }
    /// <summary>The email.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Email")]
    public string? Email { get; set; }
    /// <summary>The phone number.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "PhoneNumber")]
    public string? PhoneNumber { get; set; }
    /// <summary>The password.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Password")]
    public string? Password { get; set; }

    /// <summary>The user roles.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "UserRoles")]
    public IEnumerable<CreateUserRoleModel>? UserRoles { get; set; }

    /// <summary>Ids of the 2FA methods to enrol the new user in.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "TwoFactorAuthTypeIds")]
    public IEnumerable<int>? TwoFactorAuthTypeIds { get; set; }

    /// <summary>Consent documents the user accepts at registration.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Consents")]
    public IEnumerable<CreateUserConsentModel>? Consents { get; set; }
}

public class CreateUserModelValidator : AbstractValidator<CreateUserModel>
{
    public CreateUserModelValidator()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
        RuleFor(x => x.Username).NotNull().NotEmpty();
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}
