using GM.Identity.Sample.API.Operations;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Scopes;

/// <summary>Scope operation model.</summary>
public class ScopeOperationModel
{
    /// <summary>The id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    /// <summary>The scope.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Scope")]
    public ScopeModel? Scope { get; set; }
    /// <summary>The operation.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Operation")]
    public OperationModel? Operation { get; set; }
}
