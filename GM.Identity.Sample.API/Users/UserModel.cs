namespace GM.Identity.Sample.API.Users;

public class UserModel : AuditableModel
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
}