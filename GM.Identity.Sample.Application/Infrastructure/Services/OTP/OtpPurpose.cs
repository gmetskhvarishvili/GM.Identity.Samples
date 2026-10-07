namespace GM.Identity.Sample.Application.Infrastructure.Services.OTP;

/// <summary>
/// Well-known reasons a one-time code is issued. GM.OTP generates and validates codes
/// scoped to a purpose, so generation and confirmation must use the same value.
/// </summary>
public static class OtpPurpose
{
    public const string ConfirmUser = "ConfirmUser";

    /// <summary>A one-time code issued as the second factor of a login (grant_type=two_factor).</summary>
    public const string TwoFactor = "TwoFactor";

    /// <summary>A one-time code issued for a forgotten-password reset (ResetUserPassword -> RecoverUserPassword).</summary>
    public const string ResetPassword = "ResetPassword";
}
