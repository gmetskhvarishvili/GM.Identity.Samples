using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Groups;

/// <summary>Request body to create a group.</summary>
public class CreateGroupModel
{
    /// <summary>The name.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string Name { get; set; } = null!;
}

public class CreateGroupModelValidator : AbstractValidator<CreateGroupModel>
{
    public CreateGroupModelValidator() => RuleFor(x => x.Name).NotNull().NotEmpty();
}
