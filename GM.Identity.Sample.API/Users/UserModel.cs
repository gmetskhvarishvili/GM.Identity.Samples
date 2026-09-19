using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

public class UserModel : AuditableModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "Email")]
    public string? Email { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "UserName")]
    public string? Username { get; set; }
}
