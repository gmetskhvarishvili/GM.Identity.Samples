using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>User model.</summary>
public class UserModel : AuditableModel
{
    /// <summary>The id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    /// <summary>The email.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Email")]
    public string? Email { get; set; }
    /// <summary>The username.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "UserName")]
    public string? Username { get; set; }
}
