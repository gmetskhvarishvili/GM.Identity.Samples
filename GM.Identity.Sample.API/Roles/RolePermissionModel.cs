using GM.Identity.Sample.API.Permissions;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Roles;

public class RolePermissionModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "Role")]
    public RoleModel? Role { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "Permission")]
    public PermissionModel? Permission { get; set; }
}
