using FluentValidation;
using Microsoft.AspNetCore.Mvc;

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