using GM.Exceptions;
using GM.Identity;
using ValidationException = GM.Exceptions.ValidationException;

using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Password checks that can't be expressed as a plain FluentValidation rule. The configurable complexity policy
/// (<see cref="PasswordPolicyOptions"/>) lives in the command validators via
/// <see cref="PasswordRules.StrongPassword{T}"/>; the breach check here is a separate async call.
/// </summary>
public static class PasswordPolicy
{
    /// <summary>Throws <see cref="ValidationException"/> if the password appears in a known breach.</summary>
    public static async Task EnsureNotBreachedAsync(
        string? password, IBreachedPasswordChecker checker, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(password) && await checker.IsBreachedAsync(password, cancellationToken))
            throw new ValidationException("This password has appeared in a known data breach; choose a different one.");
    }
}
