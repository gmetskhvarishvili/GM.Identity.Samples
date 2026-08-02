using FluentValidation;

namespace GM.Identity.Sample.API.Clients;

public class CreateClientScopeModel
{
    public Guid ScopeId { get; set; }
}

public class CreateClientScopeModelValidator : AbstractValidator<CreateClientScopeModel>
{
    public CreateClientScopeModelValidator()
    {
        RuleFor(x => x.ScopeId).NotNull().NotEmpty();
    }
}