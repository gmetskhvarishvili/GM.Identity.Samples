using FluentValidation;

namespace GM.Identity.Sample.API.Roles;

public class CreateRolePermissionModel
{
    public Guid PermissionId { get; set; }
}

public class CreateRolePermissionModelValidator : AbstractValidator<CreateRolePermissionModel>
{
    public CreateRolePermissionModelValidator()
    {
        RuleFor(x => x.PermissionId).NotNull().NotEmpty();
    }
}