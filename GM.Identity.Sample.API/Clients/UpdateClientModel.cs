using FluentValidation;

namespace GM.Identity.Sample.API.Clients;

public class UpdateClientModel
{
    public string? Secret { get; set; }
    public string? Name { get; set; }
}

public class UpdateClientModelValidator : AbstractValidator<UpdateClientModel>
{
    public UpdateClientModelValidator()
    {
        RuleFor(x => x.Secret).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}