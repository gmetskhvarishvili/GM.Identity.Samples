using FluentValidation;
using GM.Mediator.Contracts;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;

namespace GM.Identity.Sample.API.Clients;

/// <summary>Create client model.</summary>
public class CreateClientModel : IRequest<string>
{
    /// <summary>The secret.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Secret")]
    public string? Secret { get; set; }
    /// <summary>The name.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
    
    /// <summary>The client scopes.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ClientScopes")]
    public IEnumerable<CreateClientScopeModel>? ClientScopes { get; set; }

    /// <summary>The exact URIs the authorization endpoint may redirect to (required for the authorization-code flow).</summary>
    [Display(ResourceType = typeof(StringResource), Name = "RedirectUris")]
    public IEnumerable<string>? RedirectUris { get; set; }

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

public class CreateClientModelValidator : AbstractValidator<CreateClientModel>
{
    public CreateClientModelValidator()
    {
        RuleFor(x => x.Secret).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleForEach(x => x.RedirectUris)
            .Must(u => string.IsNullOrWhiteSpace(u) || ClientRedirectUris.IsAbsoluteUri(u.Trim()))
            .WithMessage("Each redirect URI must be an absolute URI.");
        RuleFor(x => x.BackchannelLogoutUri)
            .Must(ClientRedirectUris.IsAbsoluteUri).When(x => !string.IsNullOrWhiteSpace(x.BackchannelLogoutUri))
            .WithMessage("The back-channel logout URI must be an absolute URI.");
        RuleFor(x => x.FrontchannelLogoutUri)
            .Must(ClientRedirectUris.IsAbsoluteUri).When(x => !string.IsNullOrWhiteSpace(x.FrontchannelLogoutUri))
            .WithMessage("The front-channel logout URI must be an absolute URI.");
    }
}
