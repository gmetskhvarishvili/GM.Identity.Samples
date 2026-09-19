using FluentValidation;

using System;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Body to create an API key: a label and an optional expiry.</summary>
public class CreateApiKeyModel
{
    /// <summary>The name.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string Name { get; set; } = null!;
    /// <summary>The expires at.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ExpiresAt")]
    public DateTime? ExpiresAt { get; set; }
}

public class CreateApiKeyModelValidator : AbstractValidator<CreateApiKeyModel>
{
    public CreateApiKeyModelValidator() => RuleFor(x => x.Name).NotNull().NotEmpty();
}
