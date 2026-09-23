using FluentValidation;
using GM.Identity;

using System.Linq;
using System.Threading;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// FluentValidation rules for a password field. <see cref="StrongPassword{T}"/> enforces the configurable
/// complexity policy (<see cref="PasswordPolicyOptions"/>) directly in the validator, so it runs in the mediator
/// validation behaviour like every other request rule. The options are injected into the command validator's
/// constructor and passed in here.
/// </summary>
public static class PasswordRules
{
    public static void StrongPassword<T>(this IRuleBuilder<T, string?> rule, PasswordPolicyOptions options)
    {
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

    /// <summary>
    /// Rejects a password found in a known breach corpus. The breach lookup is async, but the mediator validation
    /// behaviour runs validators synchronously, so this blocks on the checker. With breach checking disabled
    /// (the default no-op checker) it is instant; enabling it makes this a synchronous HTTP call per request.
    /// </summary>
    public static void NotBreached<T>(this IRuleBuilder<T, string?> rule, IBreachedPasswordChecker checker) =>
        rule.Must(password =>
                string.IsNullOrEmpty(password)
                || !checker.IsBreachedAsync(password, CancellationToken.None).GetAwaiter().GetResult())
            .WithMessage("This password has appeared in a known data breach; choose a different one.");
}
