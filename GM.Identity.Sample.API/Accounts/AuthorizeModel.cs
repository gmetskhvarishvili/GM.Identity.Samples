using FluentValidation;
using Microsoft.AspNetCore.Mvc;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Accounts;

/// <summary>Authorize model.</summary>
public class AuthorizeModel
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

    /// <summary>The grant type.</summary>
    [FromForm(Name = "grant_type")]
    [Display(ResourceType = typeof(StringResource), Name = "GrantType")]
    public string GrantType { get; set; } = null!;

    /// <summary>The refresh token.</summary>
    [FromForm(Name = "refresh_token")]
    [Display(ResourceType = typeof(StringResource), Name = "RefreshToken")]
    public string? RefreshToken { get; set; }

    /// <summary>The code.</summary>
    [FromForm(Name = "code")]
    [Display(ResourceType = typeof(StringResource), Name = "Code")]
    public string? Code { get; set; }

    /// <summary>The redirect uri.</summary>
    [FromForm(Name = "redirect_uri")]
    [Display(ResourceType = typeof(StringResource), Name = "RedirectUri")]
    public string? RedirectUri { get; set; }

    /// <summary>The code verifier.</summary>
    [FromForm(Name = "code_verifier")]
    [Display(ResourceType = typeof(StringResource), Name = "CodeVerifier")]
    public string? CodeVerifier { get; set; }

    /// <summary>The api key.</summary>
    [FromForm(Name = "api_key")]
    [Display(ResourceType = typeof(StringResource), Name = "ApiKey")]
    public string? ApiKey { get; set; }

    /// <summary>The device code.</summary>
    [FromForm(Name = "device_code")]
    [Display(ResourceType = typeof(StringResource), Name = "DeviceCode")]
    public string? DeviceCode { get; set; }
}

public class AuthorizeModelValidator : AbstractValidator<AuthorizeModel>
{
    public AuthorizeModelValidator()
    {
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ClientSecret).NotNull().NotEmpty();
        RuleFor(x => x.GrantType).NotNull().NotEmpty();
    }
}
