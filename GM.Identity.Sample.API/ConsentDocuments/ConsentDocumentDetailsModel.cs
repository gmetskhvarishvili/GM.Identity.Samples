using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.ConsentDocuments;

/// <summary>Consent document details model.</summary>
public class ConsentDocumentDetailsModel : AuditableModel
{
    /// <summary>The id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    /// <summary>The stable document identifier.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ConsentType")]
    public string? ConsentType { get; set; }
    /// <summary>The human-readable title.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Title")]
    public string? Title { get; set; }
    /// <summary>The document body (or a URL to it).</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Content")]
    public string? Content { get; set; }
    /// <summary>This row's version identifier.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Version")]
    public string? Version { get; set; }
    /// <summary>Whether an unaccepted current version is reported as pending for the user.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IsMandatory")]
    public bool IsMandatory { get; set; }
    /// <summary>Whether this is the current version for its type.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IsCurrent")]
    public bool IsCurrent { get; set; }
}
