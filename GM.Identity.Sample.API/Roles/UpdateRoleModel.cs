using FluentValidation;

namespace GM.Identity.Sample.API.Roles;

public class UpdateRoleModel
{
    public string? Name { get; set; }
}

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleModel>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}