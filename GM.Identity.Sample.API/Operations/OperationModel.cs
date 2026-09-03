namespace GM.Identity.Sample.API.Operations;

public class OperationModel : AuditableModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}