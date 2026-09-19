using FluentValidation;
using GM.Identity.Sample.Domain.Enums;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

public class ConfirmUserInitModel
{
    [Display(ResourceType = typeof(StringResource), Name = "ConfirmationType")]
    public ConfirmationType ConfirmationType { get; set; }
}

public class ConfirmUserInitModelValidator : AbstractValidator<ConfirmUserInitModel>
{
    public ConfirmUserInitModelValidator()
    {
        RuleFor(x => x.ConfirmationType).IsInEnum();
    }
}
