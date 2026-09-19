using FluentValidation;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Create user role model.</summary>
public class CreateUserRoleModel
{
    /// <summary>The role id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "RoleId")]
    public Guid RoleId { get; set; }
}

public class CreateUserRoleModelValidator : AbstractValidator<CreateUserRoleModel>
{
    public CreateUserRoleModelValidator()
    {
        RuleFor(x => x.RoleId).NotNull().NotEmpty();
    }
}
