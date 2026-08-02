using FluentValidation;

namespace GM.Identity.Sample.API.Operations;

public class CreateOperationModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class CreateOperationModelValidator : AbstractValidator<CreateOperationModel>
{
    public CreateOperationModelValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleFor(x => x.Description).NotNull().NotEmpty();
    }
}