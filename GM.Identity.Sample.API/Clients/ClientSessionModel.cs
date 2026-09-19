using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Clients;

/// <summary>Client session model.</summary>
public class ClientSessionModel : AuditableModel
{
    /// <summary>The id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    /// <summary>The client id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid ClientId { get; set; }
    /// <summary>The is revoked.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IsRevoked")]
    public bool IsRevoked { get;  set; }
    /// <summary>The revoked at.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "RevokedAt")]
    public DateTime? RevokedAt { get;  set; }
    /// <summary>The expires at.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ExpiresAt")]
    public DateTime ExpiresAt { get; set; }
}
