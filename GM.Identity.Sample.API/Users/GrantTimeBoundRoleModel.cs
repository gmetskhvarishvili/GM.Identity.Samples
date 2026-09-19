using FluentValidation;

using System;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Body for a time-bound role grant: when the temporary assignment expires.</summary>
public class GrantTimeBoundRoleModel
{
    [Display(ResourceType = typeof(StringResource), Name = "ExpiresAt")]
    public DateTime ExpiresAt { get; set; }
}

public class GrantTimeBoundRoleModelValidator : AbstractValidator<GrantTimeBoundRoleModel>
{
    public GrantTimeBoundRoleModelValidator() =>
        RuleFor(x => x.ExpiresAt).GreaterThan(_ => DateTime.UtcNow).WithMessage("The expiry must be in the future.");
}
