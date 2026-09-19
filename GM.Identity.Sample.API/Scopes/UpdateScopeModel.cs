using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Scopes;

public class UpdateScopeModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
}

public class UpdateScopeCommandValidator : AbstractValidator<UpdateScopeModel>
{
    public UpdateScopeCommandValidator()
    {
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}