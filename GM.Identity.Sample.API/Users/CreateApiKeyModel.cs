using FluentValidation;

using System;

namespace GM.Identity.Sample.API.Users;

/// <summary>Body to create an API key: a label and an optional expiry.</summary>
public class CreateApiKeyModel
{
    public string Name { get; set; } = null!;
    public DateTime? ExpiresAt { get; set; }
}

public class CreateApiKeyModelValidator : AbstractValidator<CreateApiKeyModel>
{
    public CreateApiKeyModelValidator() => RuleFor(x => x.Name).NotNull().NotEmpty();
}
