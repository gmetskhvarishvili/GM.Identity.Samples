namespace GM.Identity.Sample.Application.Infrastructure.Services.OAuth;

public class GetRedirectUriDto
{
    public string Provider { get; set; } = null!;
    public string RedirectUri { get; set; } = null!;
}