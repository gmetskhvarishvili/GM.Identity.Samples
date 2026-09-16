using Microsoft.AspNetCore.Mvc;

using System;

namespace GM.Identity.Sample.API.Accounts;

/// <summary>Form body for the device authorization endpoint (client-authenticated).</summary>
public class DeviceAuthorizationModel
{
    [FromForm(Name = "client_id")]
    public Guid ClientId { get; set; }

    [FromForm(Name = "client_secret")]
    public string ClientSecret { get; set; } = null!;

    [FromForm(Name = "scope")]
    public string? Scope { get; set; }
}

/// <summary>Form body for approving (or denying) a device user_code.</summary>
public class ApproveDeviceModel
{
    [FromForm(Name = "user_code")]
    public string UserCode { get; set; } = null!;

    [FromForm(Name = "username")]
    public string UserName { get; set; } = null!;

    [FromForm(Name = "password")]
    public string Password { get; set; } = null!;

    [FromForm(Name = "approve")]
    public bool Approve { get; set; } = true;
}
