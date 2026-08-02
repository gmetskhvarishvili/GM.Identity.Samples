using FluentValidation;

namespace GM.Identity.Sample.API.Accounts;

public class AuthConnectModel
{
    public string RedirectUri { get; set; } = null!; 
}

public class AuthConnectModelValidator : AbstractValidator<AuthConnectModel>
{
    public AuthConnectModelValidator()
    {
        RuleFor(x => x.RedirectUri).NotNull().NotEmpty();
    }
}