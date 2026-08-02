using GM.Identity.Sample.Infrastructure.Services.OTP.Models;
using Refit;

namespace GM.Identity.Sample.Infrastructure.Services.OTP;

public interface IOTPAPIService
{
    [Post("/otp/verify")]
    Task<VerifyOTPResponseModel> VerifyOTP(VerifyOTPRequestModel request, CancellationToken cancellationToken);
}