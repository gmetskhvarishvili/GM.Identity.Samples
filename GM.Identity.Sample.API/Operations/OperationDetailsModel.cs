using GM.Identity.Sample.API.Common;

using System;
namespace GM.Identity.Sample.API.Operations;

public class OperationDetailsModel : AuditableModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
