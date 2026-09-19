using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Clients;

public class ClientSessionModel : AuditableModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid ClientId { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "IsRevoked")]
    public bool IsRevoked { get;  set; }
    [Display(ResourceType = typeof(StringResource), Name = "RevokedAt")]
    public DateTime? RevokedAt { get;  set; }
    [Display(ResourceType = typeof(StringResource), Name = "ExpiresAt")]
    public DateTime ExpiresAt { get; set; }
}
