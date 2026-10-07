using GM.Identity.Sample.Application.Common;

namespace GM.Identity.Sample.Infrastructure.Options;

/// <summary>
/// Configurable password-complexity policy, bound from the <c>PasswordPolicy</c> configuration section so a
/// deployment can tighten or relax the rules without a code change. Implements <see cref="IPasswordPolicyOptions"/>
/// so the Application layer's validators can consume it. Defaults match the previous hardcoded policy
/// (min 8 with upper/lower/digit).
/// </summary>
public sealed class PasswordPolicyOptions : IPasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";

    public int MinimumLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; }

    /// <summary>When true, new passwords are also checked against the Have I Been Pwned breach corpus.</summary>
    public bool CheckForBreaches { get; set; }
}
