using GM.Exceptions;
using ValidationException = GM.Exceptions.ValidationException;

using System.Collections.Generic;
using System.Linq;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Enforces the configurable <see cref="PasswordPolicyOptions"/>. Called by every password entry point so the
/// same rules apply on create, admin reset, and self-service change.
/// </summary>
public static class PasswordPolicy
{
    /// <summary>Throws <see cref="ValidationException"/> listing every rule the password fails.</summary>
    public static void Validate(string? password, PasswordPolicyOptions options)
    {
        var failures = new List<string>();
        password ??= string.Empty;

        if (password.Length < options.MinimumLength)
            failures.Add($"be at least {options.MinimumLength} characters long");
        if (options.RequireUppercase && !password.Any(char.IsUpper))
            failures.Add("contain an uppercase letter");
        if (options.RequireLowercase && !password.Any(char.IsLower))
            failures.Add("contain a lowercase letter");
        if (options.RequireDigit && !password.Any(char.IsDigit))
            failures.Add("contain a digit");
        if (options.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
            failures.Add("contain a non-alphanumeric character");

        if (failures.Count > 0)
            throw new ValidationException($"Password must {string.Join(", ", failures)}.");
    }
}
