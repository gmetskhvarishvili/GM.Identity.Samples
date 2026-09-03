namespace GM.Identity.Sample.API.Permissions;

public class PermissionModel : AuditableModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}