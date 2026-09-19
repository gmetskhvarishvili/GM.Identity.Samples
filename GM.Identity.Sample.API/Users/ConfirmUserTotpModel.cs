using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Body for confirming an authenticator-app enrolment: a code produced by the app.</summary>
public class ConfirmUserTotpModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Code")]
    public string Code { get; set; } = null!;
}

public class ConfirmUserTotpModelValidator : AbstractValidator<ConfirmUserTotpModel>
{
    public ConfirmUserTotpModelValidator() => RuleFor(x => x.Code).NotNull().NotEmpty();
}
