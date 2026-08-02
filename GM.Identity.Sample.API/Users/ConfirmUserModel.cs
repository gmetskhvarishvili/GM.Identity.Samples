using FluentValidation;
using GM.Identity.Sample.Domain.Enums;

namespace GM.Identity.Sample.API.Users;

public class ConfirmUserModel
{
    public string Code { get; set; } = null!;
    public ConfirmationType ConfirmationType { get; set; }
}

public class ConfirmUserModelValidator : AbstractValidator<ConfirmUserModel>
{
    public ConfirmUserModelValidator()
    {
        RuleFor(x => x.Code).NotNull().NotEmpty();
        RuleFor(x => x.ConfirmationType).IsInEnum();
    }
}
