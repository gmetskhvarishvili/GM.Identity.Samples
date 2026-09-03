namespace GM.Identity.Sample.API.Clients;

public class ClientModel : AuditableModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}