using GM.Identity.Sample.API.Permissions;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Roles;

/// <summary>Role permission model.</summary>
public class RolePermissionModel
{
    /// <summary>The id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    /// <summary>The role.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Role")]
    public RoleModel? Role { get; set; }
    /// <summary>The permission.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Permission")]
    public PermissionModel? Permission { get; set; }
}
