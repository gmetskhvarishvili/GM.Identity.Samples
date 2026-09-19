using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Operations;

public class UpdateOperationModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
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