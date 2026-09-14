using FluentValidation;

namespace GM.Identity.Sample.Application.Common;

/// <summary>Shared password-complexity policy so every password entry point enforces the same rules.</summary>
public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string?> StrongPassword<T>(this IRuleBuilder<T, string?> rule) =>
        rule.NotNull().NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
}
