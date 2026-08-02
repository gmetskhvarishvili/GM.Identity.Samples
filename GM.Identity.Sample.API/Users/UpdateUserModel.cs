using FluentValidation;

namespace GM.Identity.Sample.API.Users;

public class UpdateUserModel
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class UpdateUserModelValidator : AbstractValidator<UpdateUserModel>
{
    public UpdateUserModelValidator()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
        RuleFor(x => x.Username).NotNull().NotEmpty();
    }
}