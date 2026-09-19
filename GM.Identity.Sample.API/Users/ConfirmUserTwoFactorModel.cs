using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Body for confirming a pending 2FA enrolment: the one-time setup code the user received.</summary>
public class ConfirmUserTwoFactorModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Code")]
    public string Code { get; set; } = null!;
}

public class ConfirmUserTwoFactorModelValidator : AbstractValidator<ConfirmUserTwoFactorModel>
{
    public ConfirmUserTwoFactorModelValidator() => RuleFor(x => x.Code).NotNull().NotEmpty();
}
