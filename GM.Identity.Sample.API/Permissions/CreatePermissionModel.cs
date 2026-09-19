using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Permissions;

/// <summary>Create permission model.</summary>
public class CreatePermissionModel
{
    /// <summary>The name.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
    /// <summary>The description.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Description")]
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