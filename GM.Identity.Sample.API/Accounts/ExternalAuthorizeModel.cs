using FluentValidation;

namespace GM.Identity.Sample.API.Accounts;

public class ExternalAuthorizeModel
{
    public string Code { get; set; } = null!; 
    public string State { get; set; } = null!;
    public string RedirectUri { get; set; } = null!;
    public Guid ClientId { get; set; }
    public string ClientSecret { get; set; }
}

public class ExternalAuthorizeModelValidator : AbstractValidator<ExternalAuthorizeModel>
{
    public ExternalAuthorizeModelValidator()
    {
        RuleFor(x => x.Code).NotNull().NotEmpty();
        RuleFor(x => x.State).NotNull().NotEmpty();
        RuleFor(x => x.RedirectUri).NotNull().NotEmpty();
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ClientSecret).NotNull().NotEmpty();
    }
}