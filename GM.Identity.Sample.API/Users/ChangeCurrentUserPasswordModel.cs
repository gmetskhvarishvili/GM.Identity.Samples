using FluentValidation;

namespace GM.Identity.Sample.API.Users;

public class ChangeCurrentUserPasswordModel
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

public class ChangeCurrentUserPasswordModelValidator : AbstractValidator<ChangeCurrentUserPasswordModel>
{
    public ChangeCurrentUserPasswordModelValidator()
    {
        RuleFor(x => x.CurrentPassword).NotNull().NotEmpty();
        RuleFor(x => x.NewPassword).NotNull().NotEmpty();
    }
}
