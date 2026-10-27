using GM.API.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;

namespace GM.Identity.Sample.API.Users;

/// <summary>User details model: everything about the user except the password hash and salt.</summary>
public class UserDetailsModel : AuditableModel
{
    /// <summary>The id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid Id { get; set; }
    /// <summary>The tenant the user belongs to (null for a global user).</summary>
    [Display(ResourceType = typeof(StringResource), Name = "TenantId")]
    public Guid? TenantId { get; set; }
    /// <summary>The email.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Email")]
    public string? Email { get; set; }
    /// <summary>Whether the email address has been confirmed.</summary>
    public bool EmailConfirmed { get; set; }
    /// <summary>When the email address was confirmed.</summary>
    public string? EmailConfirmedAt { get; set; }
    /// <summary>The username.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "UserName")]
    public string? Username { get; set; }
    /// <summary>The phone number.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "PhoneNumber")]
    public string? PhoneNumber { get; set; }
    /// <summary>Whether the phone number has been confirmed.</summary>
    public bool PhoneNumberConfirmed { get; set; }
    /// <summary>When the phone number was confirmed.</summary>
    public string? PhoneNumberConfirmedAt { get; set; }
    /// <summary>Whether failed sign-ins can lock the account.</summary>
    public bool LockoutEnabled { get; set; }
    /// <summary>Until when the account is locked out after failed sign-ins (null when not locked).</summary>
    public string? LockoutEnd { get; set; }
    /// <summary>Failed sign-in attempts since the last success.</summary>
    public int AccessFailedCount { get; set; }
    /// <summary>Whether an admin has blocked the account.</summary>
    public bool IsBlocked { get; set; }
    /// <summary>When the account was blocked.</summary>
    public string? BlockedAt { get; set; }
    /// <summary>The user's active roles.</summary>
    public IReadOnlyCollection<Guid> RoleIds { get; set; } = [];
    /// <summary>The user's active second-factor methods (1 Email, 2 SMS, 3 WhatsApp, or a custom type).</summary>
    [Display(ResourceType = typeof(StringResource), Name = "TwoFactorAuthTypeIds")]
    public IReadOnlyCollection<int> TwoFactorAuthTypeIds { get; set; } = [];

    /// <summary>Optional personal details, stored 1:1 with the user.</summary>
    public UserPersonalInfoModel? PersonalInfo { get; set; }

    /// <summary>Storage key of the user's profile image (e.g. an object key in blob storage), if any.</summary>
    public string? ProfileImageKey { get; set; }
}
