using GM.Identity.Sample.API.Common;

using System;
namespace GM.Identity.Sample.API.Roles;

public class RoleModel : AuditableModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}
