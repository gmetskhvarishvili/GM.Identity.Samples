using FluentValidation;
using GM.Mediator.Contracts;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
}

public class CreateClientModelValidator : AbstractValidator<CreateClientModel>
{
    public CreateClientModelValidator()
    {
        RuleFor(x => x.Secret).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}
