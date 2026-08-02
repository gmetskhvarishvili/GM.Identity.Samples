namespace GM.Identity.Sample.Application.Infrastructure.Services.OTP;

/// <summary>
/// Well-known reasons a one-time code is issued. GM.OTP generates and validates codes
/// scoped to a purpose, so generation and confirmation must use the same value.
/// </summary>
public static class OtpPurpose
{
    public const string ConfirmUser = "ConfirmUser";
}
