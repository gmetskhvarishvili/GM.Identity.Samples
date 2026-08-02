using FluentValidation;
using GM.Identity.Sample.Domain.Enums;

namespace GM.Identity.Sample.API.Users;

public class ConfirmUserInitModel
{
    public ConfirmationType ConfirmationType { get; set; }
}

public class ConfirmUserInitModelValidator : AbstractValidator<ConfirmUserInitModel>
{
    public ConfirmUserInitModelValidator()
    {
        RuleFor(x => x.ConfirmationType).IsInEnum();
    }
}
