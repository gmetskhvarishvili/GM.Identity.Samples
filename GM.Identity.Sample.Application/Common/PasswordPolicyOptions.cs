namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Configurable password-complexity policy, bound from the <c>PasswordPolicy</c> configuration section so a
/// deployment can tighten or relax the rules without a code change. Defaults match the previous hardcoded
/// policy (min 8 with upper/lower/digit).
/// </summary>
public sealed class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";

    public int MinimumLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; }
}
