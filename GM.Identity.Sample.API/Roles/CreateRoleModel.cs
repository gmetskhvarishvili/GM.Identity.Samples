using FluentValidation;

namespace GM.Identity.Sample.API.Roles;

public class CreateRoleModel
{
    public string? Name { get; set; }
    public IEnumerable<CreateRolePermissionModel>? RolePermissions { get; set; }
}

public class CreateRoleModelValidator : AbstractValidator<CreateRoleModel>
{
    public CreateRoleModelValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}