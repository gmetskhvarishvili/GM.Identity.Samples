using Microsoft.AspNetCore.Mvc;

using System;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Accounts;

/// <summary>Form body for the device authorization endpoint (client-authenticated).</summary>
public class DeviceAuthorizationModel
{
    [FromForm(Name = "client_id")]
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid ClientId { get; set; }

    [FromForm(Name = "client_secret")]
    [Display(ResourceType = typeof(StringResource), Name = "ClientSecret")]
    public string ClientSecret { get; set; } = null!;

    [FromForm(Name = "scope")]
    [Display(ResourceType = typeof(StringResource), Name = "Scope")]
    public string? Scope { get; set; }
}

/// <summary>Form body for approving (or denying) a device user_code.</summary>
public class ApproveDeviceModel
{
    [FromForm(Name = "user_code")]
    [Display(ResourceType = typeof(StringResource), Name = "UserCode")]
    public string UserCode { get; set; } = null!;

    [FromForm(Name = "username")]
    [Display(ResourceType = typeof(StringResource), Name = "UserName")]
    public string UserName { get; set; } = null!;

    [FromForm(Name = "password")]
    [Display(ResourceType = typeof(StringResource), Name = "Password")]
    public string Password { get; set; } = null!;

    [FromForm(Name = "approve")]
    [Display(ResourceType = typeof(StringResource), Name = "Approve")]
    public bool Approve { get; set; } = true;
}
