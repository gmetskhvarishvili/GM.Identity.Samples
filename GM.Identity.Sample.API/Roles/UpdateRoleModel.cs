using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Roles;

public class UpdateRoleModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
}

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleModel>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}