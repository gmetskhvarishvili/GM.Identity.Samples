using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Clients;

/// <summary>Update client model.</summary>
public class UpdateClientModel
{
    /// <summary>The secret.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Secret")]
    public string? Secret { get; set; }
    /// <summary>The name.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
}

public class UpdateClientModelValidator : AbstractValidator<UpdateClientModel>
{
    public UpdateClientModelValidator()
    {
        RuleFor(x => x.Secret).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}