using FluentValidation;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Accounts;

/// <summary>External authorize model.</summary>
public class ExternalAuthorizeModel
{
    /// <summary>The code.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Code")]
    public string Code { get; set; } = null!; 
    /// <summary>The state.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "State")]
    public string State { get; set; } = null!;
    /// <summary>The redirect uri.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "RedirectUri")]
    public string RedirectUri { get; set; } = null!;
    /// <summary>The client id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid ClientId { get; set; }
    /// <summary>The client secret.</summary>
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
