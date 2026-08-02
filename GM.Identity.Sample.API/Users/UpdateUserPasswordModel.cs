using FluentValidation;

namespace GM.Identity.Sample.API.Users;

public class UpdateUserPasswordModel
{
    public string Password { get; set; } = null!;
}

public class UpdateUserPasswordModelValidator : AbstractValidator<UpdateUserPasswordModel>
{
    public UpdateUserPasswordModelValidator()
    {
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}