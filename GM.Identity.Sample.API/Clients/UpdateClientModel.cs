using FluentValidation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;

namespace GM.Identity.Sample.API.Clients;

/// <summary>Update client model.</summary>
public class UpdateClientModel
{
    /// <summary>The name.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }

    /// <summary>The client's redirect URIs. Omit to keep them; a list replaces them (an empty list clears them).</summary>
    [Display(ResourceType = typeof(StringResource), Name = "RedirectUris")]
    public IEnumerable<string>? RedirectUris { get; set; }

    /// <summary>The client's OIDC back-channel logout endpoint. Omit to keep it; send empty to clear it.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "BackchannelLogoutUri")]
    public string? BackchannelLogoutUri { get; set; }

    /// <summary>The client's OIDC front-channel logout endpoint. Omit to keep it; send empty to clear it.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "FrontchannelLogoutUri")]
    public string? FrontchannelLogoutUri { get; set; }

    /// <summary>Whether the authorization endpoint must obtain the user's consent for this client. Omit to keep it.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "RequireConsent")]
    public bool? RequireConsent { get; set; }
}

public class UpdateClientModelValidator : AbstractValidator<UpdateClientModel>
{
    public UpdateClientModelValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleForEach(x => x.RedirectUris)
            .Must(u => string.IsNullOrWhiteSpace(u) || ClientRedirectUris.IsAbsoluteUri(u.Trim()))
            .WithMessage("Each redirect URI must be an absolute URI.");
        RuleFor(x => x.BackchannelLogoutUri)
            .Must(ClientRedirectUris.IsAbsoluteUri).When(x => !string.IsNullOrEmpty(x.BackchannelLogoutUri))
            .WithMessage("The back-channel logout URI must be an absolute URI.");
        RuleFor(x => x.FrontchannelLogoutUri)
            .Must(ClientRedirectUris.IsAbsoluteUri).When(x => !string.IsNullOrEmpty(x.FrontchannelLogoutUri))
            .WithMessage("The front-channel logout URI must be an absolute URI.");
    }
}
