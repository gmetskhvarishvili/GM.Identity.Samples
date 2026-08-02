namespace GM.Identity.Sample.Infrastructure.Services.OTP.Models;

public class VerifyOTPRequestModel
{
    public string? Subject { get; set; }
    public string? Purpose { get; set; }
    public string? Code { get; set; }
}