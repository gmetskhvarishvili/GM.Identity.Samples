using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;

namespace GM.Identity.Sample.API.Accounts;

/// <summary>Send two-factor code model.</summary>
public class SendTwoFactorCodeModel
{
    /// <summary>The user name.</summary>
    [FromForm(Name = "username")]
    [Display(ResourceType = typeof(StringResource), Name = "UserName")]
    public string? UserName { get; set; }

    /// <summary>The password.</summary>
    [FromForm(Name = "password")]
    [Display(ResourceType = typeof(StringResource), Name = "Password")]
    public string? Password { get; set; }

    /// <summary>The client id.</summary>
    [FromForm(Name = "client_id")]
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid ClientId { get; set; }

    /// <summary>The client secret.</summary>
    [FromForm(Name = "client_secret")]
    [Display(ResourceType = typeof(StringResource), Name = "ClientSecret")]
    public string ClientSecret { get; set; } = null!;

    /// <summary>The enrolled method to send the code over: 1 Email, 2 SMS, 3 WhatsApp.</summary>
    [FromForm(Name = "two_factor_auth_type_id")]
    [Display(ResourceType = typeof(StringResource), Name = "TwoFactorAuthType")]
    public int TwoFactorAuthTypeId { get; set; }
}

public class SendTwoFactorCodeModelValidator : AbstractValidator<SendTwoFactorCodeModel>
{
    public SendTwoFactorCodeModelValidator()
    {
        RuleFor(x => x.UserName).NotNull().NotEmpty();
        RuleFor(x => x.Password).NotNull().NotEmpty();
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ClientSecret).NotNull().NotEmpty();
        RuleFor(x => x.TwoFactorAuthTypeId).InclusiveBetween(1, 3);
    }
}
