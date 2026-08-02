using FluentValidation;

namespace GM.Identity.Sample.API.Scopes;

public class CreateScopeModel
{
    public string? Name { get; set; }
    public IEnumerable<CreateScopeOperationModel>? ScopeOperations { get; set; }
}

public class CreateScopeModelValidator : AbstractValidator<CreateScopeModel>
{
    public CreateScopeModelValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}