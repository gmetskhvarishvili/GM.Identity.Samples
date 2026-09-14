using FluentValidation;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Basic presence check for a password field. The full complexity policy is configurable and enforced in the
/// command handlers via <see cref="PasswordPolicy"/> (bound from <see cref="PasswordPolicyOptions"/>), so it can
/// be tuned per deployment without touching validators.
/// </summary>
public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string?> StrongPassword<T>(this IRuleBuilder<T, string?> rule) =>
        rule.NotNull().NotEmpty();
}
