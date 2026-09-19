using FluentValidation;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Scopes;

/// <summary>Create scope model.</summary>
public class CreateScopeModel
{
    /// <summary>The name.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
    /// <summary>The scope operations.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ScopeOperations")]
    public IEnumerable<CreateScopeOperationModel>? ScopeOperations { get; set; }
}

public class CreateScopeModelValidator : AbstractValidator<CreateScopeModel>
{
    public CreateScopeModelValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}
