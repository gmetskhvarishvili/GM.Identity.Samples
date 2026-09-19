using FluentValidation;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Clients;

/// <summary>Create client scope model.</summary>
public class CreateClientScopeModel
{
    /// <summary>The scope id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ScopeId")]
    public Guid ScopeId { get; set; }
}

public class CreateClientScopeModelValidator : AbstractValidator<CreateClientScopeModel>
{
    public CreateClientScopeModelValidator()
    {
        RuleFor(x => x.ScopeId).NotNull().NotEmpty();
    }
}
