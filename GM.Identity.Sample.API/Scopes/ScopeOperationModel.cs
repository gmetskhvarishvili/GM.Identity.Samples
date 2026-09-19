using GM.Identity.Sample.API.Operations;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Scopes;

public class ScopeOperationModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "Scope")]
    public ScopeModel? Scope { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "Operation")]
    public OperationModel? Operation { get; set; }
}
