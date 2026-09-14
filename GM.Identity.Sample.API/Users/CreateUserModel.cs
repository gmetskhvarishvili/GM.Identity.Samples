using FluentValidation;

using System.Collections.Generic;
namespace GM.Identity.Sample.API.Users;

public class CreateUserModel
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Password { get; set; }

    public IEnumerable<CreateUserRoleModel>? UserRoles { get; set; }

    /// <summary>Ids of the 2FA methods to enrol the new user in.</summary>
    public IEnumerable<int>? TwoFactorAuthTypeIds { get; set; }
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
