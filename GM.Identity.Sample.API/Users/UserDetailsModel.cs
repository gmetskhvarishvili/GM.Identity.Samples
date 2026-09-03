namespace GM.Identity.Sample.API.Users;

public class UserDetailsModel : AuditableModel
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
}