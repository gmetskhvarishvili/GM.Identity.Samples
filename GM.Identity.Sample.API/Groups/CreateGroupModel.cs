using FluentValidation;

namespace GM.Identity.Sample.API.Groups;

/// <summary>Request body to create a group.</summary>
public class CreateGroupModel
{
    public string Name { get; set; } = null!;
}

public class CreateGroupModelValidator : AbstractValidator<CreateGroupModel>
{
    public CreateGroupModelValidator() => RuleFor(x => x.Name).NotNull().NotEmpty();
}
