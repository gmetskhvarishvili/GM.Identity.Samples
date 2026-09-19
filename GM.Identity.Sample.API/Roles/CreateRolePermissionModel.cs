using FluentValidation;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Roles;

/// <summary>Create role permission model.</summary>
public class CreateRolePermissionModel
{
    /// <summary>The permission id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "PermissionId")]
    public Guid PermissionId { get; set; }
}

public class CreateRolePermissionModelValidator : AbstractValidator<CreateRolePermissionModel>
{
    public CreateRolePermissionModelValidator()
    {
        RuleFor(x => x.PermissionId).NotNull().NotEmpty();
    }
}
