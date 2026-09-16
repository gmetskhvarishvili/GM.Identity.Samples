using FluentValidation;
using Microsoft.AspNetCore.Mvc;

using System;
namespace GM.Identity.Sample.API.Accounts;

public class AuthorizeModel
{
    [FromForm(Name = "username")]
    public string? UserName { get; set; }
    
    [FromForm(Name = "password")]
    public string? Password { get; set; }
    
    [FromForm(Name = "client_id")]
    public Guid ClientId { get; set; }
    
    [FromForm(Name = "client_secret")]
    public string ClientSecret { get; set; } = null!;

    [FromForm(Name = "grant_type")]
    public string GrantType { get; set; } = null!;

    [FromForm(Name = "refresh_token")]
    public string? RefreshToken { get; set; }

    [FromForm(Name = "code")]
    public string? Code { get; set; }

    [FromForm(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromForm(Name = "code_verifier")]
    public string? CodeVerifier { get; set; }

    [FromForm(Name = "api_key")]
    public string? ApiKey { get; set; }

    [FromForm(Name = "device_code")]
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
