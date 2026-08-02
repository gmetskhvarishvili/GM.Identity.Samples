using FluentValidation;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.API.Clients;

public class CreateClientModel : IRequest<string>
{
    public string? Secret { get; set; }
    public string? Name { get; set; }
    
    public IEnumerable<CreateClientScopeModel>? ClientScopes { get; set; }
}

public class CreateClientModelValidator : AbstractValidator<CreateClientModel>
{
    public CreateClientModelValidator()
    {
        RuleFor(x => x.Secret).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}