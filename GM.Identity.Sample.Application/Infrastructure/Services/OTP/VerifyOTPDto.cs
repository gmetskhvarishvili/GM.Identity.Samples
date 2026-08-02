namespace GM.Identity.Sample.Application.Infrastructure.Services.OTP;

public class VerifyOTPDto
{
    /// <summary>The subject the code was issued against (e.g. the email address or phone number).</summary>
    public string Subject { get; set; } = null!;

    /// <summary>Why the code was issued (e.g. <see cref="OtpPurpose.ConfirmUser"/>).</summary>
    public string Purpose { get; set; } = null!;

    /// <summary>The one-time code submitted for validation.</summary>
    public string Code { get; set; } = null!;
}
