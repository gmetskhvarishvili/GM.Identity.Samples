using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Permissions;

/// <summary>Permission model.</summary>
public class PermissionModel : AuditableModel
{
    /// <summary>The id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    /// <summary>The name.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
    /// <summary>The description.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Description")]
    public string? Description { get; set; }
}
