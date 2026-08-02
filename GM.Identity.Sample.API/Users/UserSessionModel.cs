namespace GM.Identity.Sample.API.Users;

public class UserSessionModel
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ClientId { get; set; }
    public bool IsRevoked { get;  set; }
    public DateTime? RevokedAt { get;  set; }
    public DateTime ExpiresAt { get; set; }
}