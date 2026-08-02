using FluentValidation;

namespace GM.Identity.Sample.API.Permissions;

public class CreatePermissionModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class CreatePermissionModelValidator : AbstractValidator<CreatePermissionModel>
{
    public CreatePermissionModelValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleFor(x => x.Description).NotNull().NotEmpty();
    }
}