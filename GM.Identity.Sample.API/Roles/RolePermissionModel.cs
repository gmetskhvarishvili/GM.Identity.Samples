using GM.Identity.Sample.API.Permissions;

using System;
namespace GM.Identity.Sample.API.Roles;

public class RolePermissionModel
{
    public Guid Id { get; set; }
    public RoleModel? Role { get; set; }
    public PermissionModel? Permission { get; set; }
}
