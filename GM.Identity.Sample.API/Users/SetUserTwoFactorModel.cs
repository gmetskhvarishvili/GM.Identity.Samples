using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Body to enable or disable a user's second-factor method.</summary>
public class SetUserTwoFactorModel
{
    /// <summary>True to enrol and activate the method, false to remove it.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Enabled")]
    public bool Enabled { get; set; }
}
