using FluentValidation;

namespace GM.Identity.Sample.API.Users;

public class RecoverUserPasswordModel
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
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