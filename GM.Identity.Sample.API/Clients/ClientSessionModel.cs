namespace GM.Identity.Sample.API.Clients;

public class ClientSessionModel
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public bool IsRevoked { get;  set; }
    public DateTime? RevokedAt { get;  set; }
    public DateTime ExpiresAt { get; set; }
}