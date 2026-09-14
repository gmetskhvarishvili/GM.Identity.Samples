using GM.Identity.Sample.Infrastructure.Services.OTP.Models;
using Refit;

using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Infrastructure.Services.OTP;

public interface IOTPAPIService
{
    [Post("/otp/verify")]
    Task<VerifyOTPResponseModel> VerifyOTP(VerifyOTPRequestModel request, CancellationToken cancellationToken);
}
