namespace GM.Identity.Sample.API.Clients;

public class ClientDetailsModel : AuditableModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}