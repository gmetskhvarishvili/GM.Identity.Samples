using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Roles;

/// <summary>Update role model.</summary>
public class UpdateRoleModel
{
    /// <summary>The name.</summary>
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