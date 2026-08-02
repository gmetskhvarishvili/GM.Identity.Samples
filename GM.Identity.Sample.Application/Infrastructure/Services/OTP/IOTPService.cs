namespace GM.Identity.Sample.Application.Infrastructure.Services.OTP;

public interface IOTPService
{
    public Task VerifyOTP(VerifyOTPDto request, CancellationToken cancellationToken);
}
