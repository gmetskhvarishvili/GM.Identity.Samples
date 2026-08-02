using FluentValidation;

namespace GM.Identity.Sample.API.Scopes;

public class UpdateScopeModel
{
    public string? Name { get; set; }
}

public class UpdateScopeCommandValidator : AbstractValidator<UpdateScopeModel>
{
    public UpdateScopeCommandValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}