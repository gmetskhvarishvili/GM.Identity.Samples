using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Infrastructure.Services.OAuth;

public interface IOAuthService
{
    public Task<string> GetRedirectUri(GetRedirectUriDto request, CancellationToken cancellationToken);

    public Task<string> GetEmail(GetEmailDto request, CancellationToken cancellationToken);
}
