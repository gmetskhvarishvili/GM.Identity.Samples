namespace GM.Identity.Sample.API.Scopes;

public class ScopeDetailsModel : AuditableModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}