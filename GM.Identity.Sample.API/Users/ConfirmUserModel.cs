using FluentValidation;
using GM.Identity.Sample.Domain.Enums;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

public class ConfirmUserModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Code")]
    public string Code { get; set; } = null!;
    [Display(ResourceType = typeof(StringResource), Name = "ConfirmationType")]
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
