using FluentValidation;

namespace GM.Identity.Sample.API.Users;

/// <summary>Body for confirming an authenticator-app enrolment: a code produced by the app.</summary>
public class ConfirmUserTotpModel
{
    public string Code { get; set; } = null!;
}

public class ConfirmUserTotpModelValidator : AbstractValidator<ConfirmUserTotpModel>
{
    public ConfirmUserTotpModelValidator() => RuleFor(x => x.Code).NotNull().NotEmpty();
}
