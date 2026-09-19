using FluentValidation;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Roles;

/// <summary>Create role model.</summary>
public class CreateRoleModel
{
    /// <summary>The name.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
    /// <summary>The role permissions.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "RolePermissions")]
    public IEnumerable<CreateRolePermissionModel>? RolePermissions { get; set; }
}

public class CreateRoleModelValidator : AbstractValidator<CreateRoleModel>
{
    public CreateRoleModelValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}
