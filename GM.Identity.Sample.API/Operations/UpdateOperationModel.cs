using FluentValidation;

namespace GM.Identity.Sample.API.Operations;

public class UpdateOperationModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class UpdateOperationModelValidator : AbstractValidator<UpdateOperationModel>
{
    public UpdateOperationModelValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleFor(x => x.Description).NotNull().NotEmpty();
    }
}