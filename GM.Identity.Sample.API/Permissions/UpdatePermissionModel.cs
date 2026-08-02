using FluentValidation;

namespace GM.Identity.Sample.API.Permissions;

public class UpdatePermissionModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class UpdatePermissionModelValidator : AbstractValidator<UpdatePermissionModel>
{
    public UpdatePermissionModelValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleFor(x => x.Description).NotNull().NotEmpty();
    }
}