namespace GM.Identity.Sample.Infrastructure.Services.OTP.Models;

public class VerifyOTPResponseModel
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public Guid? ChallengeId { get; set; }
}