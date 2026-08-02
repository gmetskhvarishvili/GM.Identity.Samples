using FluentValidation;

namespace GM.Identity.Sample.API.Users;

public class CreateUserRoleModel
{
    public Guid RoleId { get; set; }
}

public class CreateUserRoleModelValidator : AbstractValidator<CreateUserRoleModel>
{
    public CreateUserRoleModelValidator()
    {
        RuleFor(x => x.RoleId).NotNull().NotEmpty();
    }
}