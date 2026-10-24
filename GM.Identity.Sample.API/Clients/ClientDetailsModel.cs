using GM.API.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;

namespace GM.Identity.Sample.API.Clients;

/// <summary>Client details model.</summary>
public class ClientDetailsModel : AuditableModel
{
    /// <summary>The id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    /// <summary>The name.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
    /// <summary>The exact URIs the authorization endpoint may redirect to.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "RedirectUris")]
    public IReadOnlyList<string> RedirectUris { get; set; } = [];
    /// <summary>The client's OIDC back-channel logout endpoint, if any.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "BackchannelLogoutUri")]
    public string? BackchannelLogoutUri { get; set; }
    /// <summary>The client's OIDC front-channel logout endpoint, if any.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "FrontchannelLogoutUri")]
    public string? FrontchannelLogoutUri { get; set; }
    /// <summary>Whether the authorization endpoint must obtain the user's consent for this client.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "RequireConsent")]
    public bool RequireConsent { get; set; }
}
