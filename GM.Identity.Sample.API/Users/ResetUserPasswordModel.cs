using FluentValidation;

namespace GM.Identity.Sample.API.Users;

public class ResetUserPasswordModel
{
    public string Email { get; set; } = null!;
    public int NotificationType { get; set; }
}

public class ResetUserPasswordModelValidator : AbstractValidator<ResetUserPasswordModel>
{
    public ResetUserPasswordModelValidator()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty();
        RuleFor(x => x.NotificationType).NotNull();
    }
}