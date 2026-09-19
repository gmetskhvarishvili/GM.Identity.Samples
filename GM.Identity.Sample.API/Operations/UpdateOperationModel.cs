using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Operations;

/// <summary>Update operation model.</summary>
public class UpdateOperationModel
{
    /// <summary>The name.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
    /// <summary>The description.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Description")]
    public string? Description { get; set; }
}

public class UpdateOperationModelValidator : AbstractValidator<UpdateOperationModel>
{
    public UpdateOperationModelValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
        RuleFor(x => x.Description).NotNull().NotEmpty();
    }
}