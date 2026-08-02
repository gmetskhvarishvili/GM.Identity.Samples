namespace GM.Identity.Sample.Application.Infrastructure.Services.OAuth;

public interface IOAuthService
{
    public string GetRedirectUri(GetRedirectUriDto request);

    public Task<string> GetEmail(GetEmailDto request, CancellationToken cancellationToken);
}