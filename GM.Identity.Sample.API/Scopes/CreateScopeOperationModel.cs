using FluentValidation;

namespace GM.Identity.Sample.API.Scopes;

public class CreateScopeOperationModel
{
    public Guid OperationId { get; set; }
}

public class CreateScopeOperationModelValidator : AbstractValidator<CreateScopeOperationModel>
{
    public CreateScopeOperationModelValidator()
    {
        RuleFor(x => x.OperationId).NotNull().NotEmpty();
    }
}