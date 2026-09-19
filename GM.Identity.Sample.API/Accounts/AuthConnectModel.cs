using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Accounts;

public class AuthConnectModel
{
    [Display(ResourceType = typeof(StringResource), Name = "RedirectUri")]
    public string RedirectUri { get; set; } = null!; 
}

public class AuthConnectModelValidator : AbstractValidator<AuthConnectModel>
{
    public AuthConnectModelValidator()
    {
        RuleFor(x => x.RedirectUri).NotNull().NotEmpty();
    }
}