using FluentValidation;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Accounts;

public class ExternalAuthorizeModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Code")]
    public string Code { get; set; } = null!; 
    [Display(ResourceType = typeof(StringResource), Name = "State")]
    public string State { get; set; } = null!;
    [Display(ResourceType = typeof(StringResource), Name = "RedirectUri")]
    public string RedirectUri { get; set; } = null!;
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid ClientId { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "ClientSecret")]
    public string ClientSecret { get; set; } = null!;
}

public class ExternalAuthorizeModelValidator : AbstractValidator<ExternalAuthorizeModel>
{
    public ExternalAuthorizeModelValidator()
    {
        RuleFor(x => x.Code).NotNull().NotEmpty();
        RuleFor(x => x.State).NotNull().NotEmpty();
        RuleFor(x => x.RedirectUri).NotNull().NotEmpty();
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ClientSecret).NotNull().NotEmpty();
    }
}
