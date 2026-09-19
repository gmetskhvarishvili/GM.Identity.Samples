using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Scopes;

public class ScopeModel : AuditableModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
}
