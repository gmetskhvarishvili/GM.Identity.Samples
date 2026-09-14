using GM.Identity.Sample.API.Common;

using System;
namespace GM.Identity.Sample.API.Scopes;

public class ScopeModel : AuditableModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}
