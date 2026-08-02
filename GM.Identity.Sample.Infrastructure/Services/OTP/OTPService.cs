using GM.Exceptions;
using GM.Identity.Sample.Application.Infrastructure.Services.OTP;
using GM.Identity.Sample.Infrastructure.Services.OTP.Models;

namespace GM.Identity.Sample.Infrastructure.Services.OTP;

public class OTPService(IOTPAPIService otpAPIService) : IOTPService
{
    public async Task VerifyOTP(VerifyOTPDto request, CancellationToken cancellationToken)
    {
        var requestModel = new VerifyOTPRequestModel
        {
           Subject = request.Subject,
           Purpose = request.Purpose,
           Code = request.Code
        };
        
        var responseModel = await otpAPIService.VerifyOTP(requestModel, cancellationToken);

        if (!responseModel.IsValid)
        {
            throw new ValidationException("OTP verification failed");
        }
    }
}
