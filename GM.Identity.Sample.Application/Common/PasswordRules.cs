using FluentValidation;

using System.Linq;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// FluentValidation rules for a password field. <see cref="StrongPassword{T}"/> enforces the configurable
/// complexity policy (<see cref="PasswordPolicyOptions"/>) directly in the validator, so it runs in the mediator
/// validation behaviour like every other request rule. The options are injected into the command validator's
/// constructor and passed in here.
/// </summary>
public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string?> StrongPassword<T>(
        this IRuleBuilder<T, string?> rule, PasswordPolicyOptions options) =>
        rule
            .NotNull().NotEmpty()
            .MinimumLength(options.MinimumLength)
            .Must(p => !options.RequireUppercase || (p != null && p.Any(char.IsUpper)))
                .WithMessage("'{PropertyName}' must contain an uppercase letter.")
            .Must(p => !options.RequireLowercase || (p != null && p.Any(char.IsLower)))
                .WithMessage("'{PropertyName}' must contain a lowercase letter.")
            .Must(p => !options.RequireDigit || (p != null && p.Any(char.IsDigit)))
                .WithMessage("'{PropertyName}' must contain a digit.")
            .Must(p => !options.RequireNonAlphanumeric || (p != null && p.Any(c => !char.IsLetterOrDigit(c))))
                .WithMessage("'{PropertyName}' must contain a non-alphanumeric character.");
}
