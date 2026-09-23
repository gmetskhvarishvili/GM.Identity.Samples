namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Password-complexity policy consumed by the Application layer's password validators. The concrete options class
/// that binds the <c>PasswordPolicy</c> configuration section lives in Infrastructure and implements this
/// interface; Infrastructure registers it so the validators can depend on the policy without referencing Infrastructure.
/// </summary>
public interface IPasswordPolicyOptions
{
    /// <summary>Minimum password length.</summary>
    int MinimumLength { get; }

    /// <summary>Require at least one uppercase letter.</summary>
    bool RequireUppercase { get; }

    /// <summary>Require at least one lowercase letter.</summary>
    bool RequireLowercase { get; }

    /// <summary>Require at least one digit.</summary>
    bool RequireDigit { get; }

    /// <summary>Require at least one non-alphanumeric character.</summary>
    bool RequireNonAlphanumeric { get; }

    /// <summary>When true, new passwords are also checked against a breach corpus.</summary>
    bool CheckForBreaches { get; }
}
