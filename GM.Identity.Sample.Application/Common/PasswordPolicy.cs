using GM.Exceptions;
using GM.Identity;
using ValidationException = GM.Exceptions.ValidationException;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    /// <summary>Throws <see cref="ValidationException"/> if the password appears in a known breach.</summary>
    public static async Task EnsureNotBreachedAsync(
        string? password, IBreachedPasswordChecker checker, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(password) && await checker.IsBreachedAsync(password, cancellationToken))
            throw new ValidationException("This password has appeared in a known data breach; choose a different one.");
    }
}
