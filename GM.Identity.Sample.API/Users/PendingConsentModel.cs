using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>A consent document the current user must still accept.</summary>
public class PendingConsentModel
{
    /// <summary>The stable document identifier.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ConsentType")]
    public string ConsentType { get; set; } = null!;
    /// <summary>The human-readable title.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Title")]
    public string Title { get; set; } = null!;
    /// <summary>The document body (or a URL to it).</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Content")]
    public string Content { get; set; } = null!;
    /// <summary>The version to accept.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Version")]
    public string Version { get; set; } = null!;
}
